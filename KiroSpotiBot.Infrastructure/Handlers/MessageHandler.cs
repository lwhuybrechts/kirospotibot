using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KiroSpotiBot.Infrastructure.Handlers;

/// <summary>
/// Handles message processing logic for Telegram messages.
/// </summary>
public class MessageHandler : IMessageHandler
{
    private readonly ILogger<MessageHandler> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IUserRepository _userRepository;
    private readonly IGroupChatRepository _groupChatRepository;
    private readonly ISpotifyUrlDetector _spotifyUrlDetector;
    private readonly ITrackMetadataService _trackMetadataService;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly ISpotifyService _spotifyService;

    public MessageHandler(
        ILogger<MessageHandler> logger,
        ITelegramBotClient telegramBotClient,
        IUserRepository userRepository,
        IGroupChatRepository groupChatRepository,
        ISpotifyUrlDetector spotifyUrlDetector,
        ITrackMetadataService trackMetadataService,
        ITrackRecordRepository trackRecordRepository,
        ISpotifyService spotifyService)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _userRepository = userRepository;
        _groupChatRepository = groupChatRepository;
        _spotifyUrlDetector = spotifyUrlDetector;
        _trackMetadataService = trackMetadataService;
        _trackRecordRepository = trackRecordRepository;
        _spotifyService = spotifyService;
    }

    /// <summary>
    /// Handles incoming text messages from Telegram.
    /// </summary>
    public async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure message has text content.
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                _logger.LogDebug("Ignoring message {MessageId} with no text content.", message.MessageId);
                return;
            }

            // Ensure user exists in database (create if needed).
            var user = await _userRepository.GetByTelegramUserIdAsync(message.From!.Id, cancellationToken);
            if (user == null)
            {
                _logger.LogInformation("Creating new user record for Telegram user {UserId}.", message.From.Id);
                user = await _userRepository.CreateUserAsync(message.From.Id, cancellationToken);
            }

            // Detect Spotify URLs in message text.
            var spotifyUrls = _spotifyUrlDetector.DetectTrackUrls(message.Text);
            if (!spotifyUrls.Any())
            {
                _logger.LogDebug("No Spotify URLs detected in message {MessageId}.", message.MessageId);
                return;
            }

            _logger.LogInformation("Detected {Count} Spotify URL(s) in message {MessageId}.", 
                spotifyUrls.Count(), message.MessageId);

            // Retrieve group configuration from Table Storage.
            var groupChat = await _groupChatRepository.GetByTelegramChatIdAsync(message.Chat.Id, cancellationToken);
            if (groupChat == null)
            {
                _logger.LogWarning("No group chat configuration found for chat {ChatId}. Bot may not be properly configured.", 
                    message.Chat.Id);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ This group is not configured yet. Please wait for the administrator to set up the bot.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Check configuration state and send appropriate prompts.
            var configurationComplete = await ValidateConfigurationStateAsync(groupChat, message.Chat.Id, cancellationToken);
            if (!configurationComplete)
            {
                _logger.LogInformation("Configuration incomplete for group {ChatId}. Prompting administrator.", 
                    message.Chat.Id);
                return;
            }

            // Process each detected Spotify URL.
            foreach (var url in spotifyUrls)
            {
                var trackId = _spotifyUrlDetector.ExtractTrackId(url);
                if (string.IsNullOrWhiteSpace(trackId))
                {
                    _logger.LogWarning("Failed to extract track ID from URL: {Url}", url);
                    continue;
                }

                await ProcessTrackAdditionAsync(
                    trackId, 
                    groupChat, 
                    message.From!.Id, 
                    message.MessageId, 
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message {MessageId} from chat {ChatId}.", 
                message.MessageId, message.Chat.Id);
            throw;
        }
    }

    /// <summary>
    /// Processes the addition of a track to the playlist.
    /// </summary>
    private async Task ProcessTrackAdditionAsync(
        string trackId,
        Core.Entities.GroupChatEntity groupChat,
        long sharedByUserId,
        int messageId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if track was previously deleted.
            var isDeleted = await _trackRecordRepository.IsTrackDeletedAsync(
                groupChat.TelegramChatId, 
                trackId, 
                cancellationToken);

            if (isDeleted)
            {
                _logger.LogInformation("Track {TrackId} was previously deleted in group {ChatId}.", 
                    trackId, groupChat.TelegramChatId);

                await _telegramBotClient.SendMessage(
                    chatId: groupChat.TelegramChatId,
                    text: "⛔ This track was previously removed due to downvotes and cannot be added again.",
                    replyParameters: new Telegram.Bot.Types.ReplyParameters { MessageId = messageId },
                    cancellationToken: cancellationToken);
                return;
            }

            // Check if track already exists in playlist (duplicate detection).
            var trackExists = await _trackRecordRepository.TrackExistsAsync(
                groupChat.TelegramChatId, 
                trackId, 
                cancellationToken);

            var isDuplicate = trackExists;

            // Fetch and store track metadata.
            var metadata = await _trackMetadataService.FetchAndStoreTrackMetadataAsync(trackId, cancellationToken);
            if (metadata == null)
            {
                _logger.LogWarning("Failed to fetch metadata for track {TrackId}.", trackId);

                await _telegramBotClient.SendMessage(
                    chatId: groupChat.TelegramChatId,
                    text: "❌ Failed to fetch track information from Spotify. Please try again.",
                    replyParameters: new Telegram.Bot.Types.ReplyParameters { MessageId = messageId },
                    cancellationToken: cancellationToken);
                return;
            }

            // Get administrator's access token.
            var adminAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(
                groupChat.AdministratorTelegramUserId, 
                cancellationToken);

            if (string.IsNullOrWhiteSpace(adminAccessToken))
            {
                _logger.LogWarning("Administrator {UserId} has no valid access token.", 
                    groupChat.AdministratorTelegramUserId);

                await _telegramBotClient.SendMessage(
                    chatId: groupChat.TelegramChatId,
                    text: "❌ Administrator authentication expired. Please re-authenticate using /auth.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Add track to Spotify playlist (only if not a duplicate).
            var addedToPlaylist = false;
            if (!isDuplicate)
            {
                if (string.IsNullOrWhiteSpace(groupChat.PlaylistId))
                {
                    _logger.LogWarning("Group {ChatId} has no playlist configured.", groupChat.TelegramChatId);

                    await _telegramBotClient.SendMessage(
                        chatId: groupChat.TelegramChatId,
                        text: "❌ No playlist configured. Administrator must configure a playlist using /configure.",
                        replyParameters: new Telegram.Bot.Types.ReplyParameters { MessageId = messageId },
                        cancellationToken: cancellationToken);
                    return;
                }

                addedToPlaylist = await _spotifyService.AddTrackToPlaylistAsync(
                    groupChat.PlaylistId, 
                    trackId, 
                    adminAccessToken, 
                    cancellationToken);

                if (!addedToPlaylist)
                {
                    _logger.LogWarning("Failed to add track {TrackId} to playlist {PlaylistId}.", 
                        trackId, groupChat.PlaylistId);

                    await _telegramBotClient.SendMessage(
                        chatId: groupChat.TelegramChatId,
                        text: "❌ Failed to add track to playlist. Please check administrator authentication.",
                        replyParameters: new Telegram.Bot.Types.ReplyParameters { MessageId = messageId },
                        cancellationToken: cancellationToken);
                    return;
                }
            }

            // Get user information for denormalization.
            var user = await _userRepository.GetByTelegramUserIdAsync(sharedByUserId, cancellationToken);

            // Create TrackRecord in Table Storage.
            var trackRecord = new Core.Entities.TrackRecordEntity(
                groupChat.TelegramChatId, 
                trackId, 
                sharedByUserId)
            {
                TrackName = metadata.Name,
                ArtistName = metadata.ArtistName,
                AlbumName = metadata.AlbumName,
                AlbumImageUrl = metadata.AlbumImageUrl,
                SharedByUsername = user?.TelegramAvatarUrl ?? string.Empty,
                SharedByAvatarUrl = user?.TelegramAvatarUrl,
                TelegramMessageId = messageId,
                IsDuplicate = isDuplicate
            };

            await _trackRecordRepository.CreateTrackRecordAsync(trackRecord, cancellationToken);

            _logger.LogInformation("Created track record for {TrackId} in group {ChatId}. Duplicate: {IsDuplicate}", 
                trackId, groupChat.TelegramChatId, isDuplicate);

            // Send confirmation reply with playlist link.
            var playlistUrl = $"https://open.spotify.com/playlist/{groupChat.PlaylistId}";
            var confirmationMessage = isDuplicate
                ? $"ℹ️ **{metadata.Name}** by {metadata.ArtistName}\n\n" +
                  $"This track is already in the playlist!\n\n" +
                  $"🎵 [View Playlist]({playlistUrl})"
                : $"✅ **{metadata.Name}** by {metadata.ArtistName}\n\n" +
                  $"Added to the playlist!\n\n" +
                  $"🎵 [View Playlist]({playlistUrl})\n\n" +
                  $"Vote with 👍 or 👎 reactions!";

            await _telegramBotClient.SendMessage(
                chatId: groupChat.TelegramChatId,
                text: confirmationMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyParameters: new Telegram.Bot.Types.ReplyParameters { MessageId = messageId },
                linkPreviewOptions: new Telegram.Bot.Types.LinkPreviewOptions { IsDisabled = true },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Sent confirmation message for track {TrackId} in group {ChatId}.", 
                trackId, groupChat.TelegramChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing track addition for {TrackId} in group {ChatId}.", 
                trackId, groupChat.TelegramChatId);
            
            await _telegramBotClient.SendMessage(
                chatId: groupChat.TelegramChatId,
                text: "❌ An error occurred while processing the track. Please try again later.",
                replyParameters: new Telegram.Bot.Types.ReplyParameters { MessageId = messageId },
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Handles bot being added to a group chat.
    /// </summary>
    public async Task HandleBotAddedToGroupAsync(ChatMemberUpdated update, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Bot was added to group {ChatId} by user {UserId}.", 
                update.Chat.Id, update.From.Id);

            // Create group chat record in Table Storage.
            var groupChat = await _groupChatRepository.CreateGroupChatAsync(
                update.Chat.Id, 
                update.From.Id, 
                cancellationToken);

            _logger.LogInformation("Created group chat record for {ChatId} with administrator {UserId}.", 
                groupChat.TelegramChatId, groupChat.AdministratorTelegramUserId);

            // Send welcome message explaining administrator privileges.
            var welcomeMessage = "👋 Hello! I'm KiroSpotiBot. I'll help you build collaborative Spotify playlists.\n\n" +
                                "🎵 **How it works:**\n" +
                                "• Share Spotify track links in this chat\n" +
                                "• I'll automatically add them to your configured playlist\n" +
                                "• Vote on tracks with 👍 or 👎 reactions\n" +
                                "• Tracks with too many downvotes are removed automatically\n\n" +
                                $"👤 **Administrator:** <a href=\"tg://user?id={update.From.Id}\">{update.From.FirstName}</a>\n" +
                                "The administrator can configure the bot using these commands:\n" +
                                "• /auth - Authenticate with Spotify\n" +
                                "• /configure - Set the target playlist\n" +
                                "• /threshold - Set downvote threshold\n\n" +
                                "⚙️ **Setup required:**\n" +
                                "1. Administrator must authenticate with Spotify using /auth\n" +
                                "2. Administrator must configure a playlist using /configure\n\n" +
                                "Once configured, start sharing Spotify links!";

            await _telegramBotClient.SendMessage(
                chatId: update.Chat.Id,
                text: welcomeMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Sent welcome message to group {ChatId}.", update.Chat.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling bot added to group {ChatId}.", update.Chat.Id);
            throw;
        }
    }

    /// <summary>
    /// Validates the configuration state of a group chat and sends prompts if incomplete.
    /// </summary>
    /// <returns>True if configuration is complete, false otherwise.</returns>
    private async Task<bool> ValidateConfigurationStateAsync(
        Core.Entities.GroupChatEntity groupChat, 
        long chatId, 
        CancellationToken cancellationToken)
    {
        var missingConfiguration = new List<string>();

        // Check if administrator has authenticated with Spotify.
        var administrator = await _userRepository.GetByTelegramUserIdAsync(
            groupChat.AdministratorTelegramUserId, 
            cancellationToken);

        var hasSpotifyAuth = administrator != null && 
                            !string.IsNullOrWhiteSpace(administrator.EncryptedAccessToken);

        if (!hasSpotifyAuth)
        {
            missingConfiguration.Add("🔐 Administrator needs to authenticate with Spotify using /auth");
        }

        // Check if playlist is configured.
        var hasPlaylist = !string.IsNullOrWhiteSpace(groupChat.PlaylistId);
        if (!hasPlaylist)
        {
            missingConfiguration.Add("🎵 Administrator needs to configure a playlist using /configure");
        }

        // If configuration is incomplete, send prompt and prevent track addition.
        if (missingConfiguration.Any())
        {
            var promptMessage = "⚠️ **Configuration Required**\n\n" +
                               "Before I can add tracks, the administrator must complete the setup:\n\n" +
                               string.Join("\n", missingConfiguration) +
                               "\n\nOnce configured, you can start sharing Spotify links!";

            await _telegramBotClient.SendMessage(
                chatId: chatId,
                text: promptMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken);

            return false;
        }

        return true;
    }
}
