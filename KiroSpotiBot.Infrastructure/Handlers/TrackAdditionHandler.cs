using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace KiroSpotiBot.Infrastructure.Handlers;

/// <summary>
/// Handles track addition to playlists.
/// </summary>
public class TrackAdditionHandler : ITrackAdditionHandler
{
    private readonly ILogger<TrackAdditionHandler> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IUserRepository _userRepository;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly ITrackMetadataService _trackMetadataService;
    private readonly ISpotifyService _spotifyService;
    private readonly IAutoQueueService _autoQueueService;

    public TrackAdditionHandler(
        ILogger<TrackAdditionHandler> logger,
        ITelegramBotClient telegramBotClient,
        IUserRepository userRepository,
        ITrackRecordRepository trackRecordRepository,
        ITrackMetadataService trackMetadataService,
        ISpotifyService spotifyService,
        IAutoQueueService autoQueueService)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _userRepository = userRepository;
        _trackRecordRepository = trackRecordRepository;
        _trackMetadataService = trackMetadataService;
        _spotifyService = spotifyService;
        _autoQueueService = autoQueueService;
    }

    /// <summary>
    /// Processes the addition of a track to the playlist.
    /// </summary>
    public async Task ProcessTrackAdditionAsync(
        string trackId,
        GroupChatEntity groupChat,
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
            var trackRecord = new TrackRecordEntity(
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

            // Trigger auto-queue for users with auto-queue enabled (only if track was actually added, not duplicate).
            if (!isDuplicate)
            {
                await _autoQueueService.TriggerAutoQueueAsync(
                    groupChat.TelegramChatId,
                    trackId,
                    cancellationToken);
            }
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
}
