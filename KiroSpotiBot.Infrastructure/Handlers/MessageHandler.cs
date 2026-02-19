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
    private readonly IGroupConfigurationValidator _configurationValidator;
    private readonly ITrackAdditionHandler _trackAdditionHandler;
    private readonly IGroupSetupHandler _groupSetupHandler;
    private readonly IVoteManager _voteManager;
    private readonly ITrackRecordRepository _trackRecordRepository;

    public MessageHandler(
        ILogger<MessageHandler> logger,
        ITelegramBotClient telegramBotClient,
        IUserRepository userRepository,
        IGroupChatRepository groupChatRepository,
        ISpotifyUrlDetector spotifyUrlDetector,
        IGroupConfigurationValidator configurationValidator,
        ITrackAdditionHandler trackAdditionHandler,
        IGroupSetupHandler groupSetupHandler,
        IVoteManager voteManager,
        ITrackRecordRepository trackRecordRepository)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _userRepository = userRepository;
        _groupChatRepository = groupChatRepository;
        _spotifyUrlDetector = spotifyUrlDetector;
        _configurationValidator = configurationValidator;
        _trackAdditionHandler = trackAdditionHandler;
        _groupSetupHandler = groupSetupHandler;
        _voteManager = voteManager;
        _trackRecordRepository = trackRecordRepository;
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
            var configurationComplete = await _configurationValidator.ValidateAndPromptAsync(
                groupChat, 
                message.Chat.Id, 
                cancellationToken);
            
            if (!configurationComplete)
            {
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

                await _trackAdditionHandler.ProcessTrackAdditionAsync(
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
    /// Handles bot being added to a group chat.
    /// </summary>
    public async Task HandleBotAddedToGroupAsync(ChatMemberUpdated update, CancellationToken cancellationToken)
    {
        await _groupSetupHandler.HandleBotAddedToGroupAsync(update, cancellationToken);
    }

    /// <summary>
    /// Handles message reaction updates (upvotes/downvotes).
    /// </summary>
    public async Task HandleMessageReactionAsync(MessageReactionUpdated reaction, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure we have a user.
            if (reaction.User == null)
            {
                _logger.LogWarning("Received reaction update without user information.");
                return;
            }

            // Get the track record associated with this message.
            var trackRecords = await _trackRecordRepository.GetByGroupChatAsync(
                reaction.Chat.Id, 
                skip: 0, 
                take: 1000, 
                cancellationToken);

            var trackRecord = trackRecords.FirstOrDefault(tr => tr.TelegramMessageId == reaction.MessageId);
            if (trackRecord == null)
            {
                _logger.LogDebug("No track record found for message {MessageId} in chat {ChatId}.", 
                    reaction.MessageId, reaction.Chat.Id);
                return;
            }

            // Check if track is deleted.
            if (trackRecord.IsDeleted)
            {
                _logger.LogInformation("Ignoring reaction on deleted track {TrackRecordId}.", trackRecord.TrackRecordId);
                return;
            }

            // Ensure user exists in database.
            var user = await _userRepository.GetByTelegramUserIdAsync(reaction.User.Id, cancellationToken);
            if (user == null)
            {
                _logger.LogInformation("Creating new user record for Telegram user {UserId}.", reaction.User.Id);
                user = await _userRepository.CreateUserAsync(reaction.User.Id, cancellationToken);
            }

            // Process new reactions (added).
            foreach (var reactionType in reaction.NewReaction)
            {
                string? voteType = null;

                // Check for thumbs up emoji.
                if (reactionType.Type == Telegram.Bot.Types.Enums.ReactionTypeKind.Emoji)
                {
                    var emojiReaction = reactionType as Telegram.Bot.Types.ReactionTypeEmoji;
                    if (emojiReaction?.Emoji == "👍")
                    {
                        voteType = "Upvote";
                    }
                    else if (emojiReaction?.Emoji == "👎")
                    {
                        voteType = "Downvote";
                    }
                }

                if (voteType != null)
                {
                    _logger.LogInformation("User {UserId} added {VoteType} to track {TrackRecordId}.", 
                        reaction.User.Id, voteType, trackRecord.TrackRecordId);

                    var trackRemoved = await _voteManager.RecordVoteAsync(
                        trackRecord.TrackRecordId,
                        reaction.Chat.Id,
                        reaction.User.Id,
                        voteType,
                        reaction.User.Username ?? reaction.User.FirstName,
                        null, // Avatar URL not available in reaction update.
                        cancellationToken);

                    if (trackRemoved)
                    {
                        // Send notification that track was removed.
                        await _telegramBotClient.SendMessage(
                            chatId: reaction.Chat.Id,
                            text: $"🗑️ Track \"{trackRecord.TrackName}\" by {trackRecord.ArtistName} was removed from the playlist due to reaching the downvote threshold.",
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        // Update confirmation message with new vote counts.
                        var voteCounts = await _voteManager.GetVoteCountsAsync(trackRecord.TrackRecordId, cancellationToken);
                        
                        try
                        {
                            await _telegramBotClient.EditMessageText(
                                chatId: reaction.Chat.Id,
                                messageId: reaction.MessageId,
                                text: $"✅ Added \"{trackRecord.TrackName}\" by {trackRecord.ArtistName} to the playlist!\n\n" +
                                      $"👍 {voteCounts.upvotes} | 👎 {voteCounts.downvotes}",
                                cancellationToken: cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to update message {MessageId} with vote counts.", reaction.MessageId);
                        }
                    }
                }
            }

            // Process removed reactions.
            foreach (var reactionType in reaction.OldReaction)
            {
                bool isVoteReaction = false;

                // Check if it's a vote reaction being removed.
                if (reactionType.Type == Telegram.Bot.Types.Enums.ReactionTypeKind.Emoji)
                {
                    var emojiReaction = reactionType as Telegram.Bot.Types.ReactionTypeEmoji;
                    if (emojiReaction?.Emoji == "👍" || emojiReaction?.Emoji == "👎")
                    {
                        isVoteReaction = true;
                    }
                }

                if (isVoteReaction)
                {
                    _logger.LogInformation("User {UserId} removed vote from track {TrackRecordId}.", 
                        reaction.User.Id, trackRecord.TrackRecordId);

                    await _voteManager.RemoveVoteAsync(
                        trackRecord.TrackRecordId,
                        reaction.Chat.Id,
                        reaction.User.Id,
                        cancellationToken);

                    // Update confirmation message with new vote counts.
                    var voteCounts = await _voteManager.GetVoteCountsAsync(trackRecord.TrackRecordId, cancellationToken);
                    
                    try
                    {
                        await _telegramBotClient.EditMessageText(
                            chatId: reaction.Chat.Id,
                            messageId: reaction.MessageId,
                            text: $"✅ Added \"{trackRecord.TrackName}\" by {trackRecord.ArtistName} to the playlist!\n\n" +
                                  $"👍 {voteCounts.upvotes} | 👎 {voteCounts.downvotes}",
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update message {MessageId} with vote counts.", reaction.MessageId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message reaction for message {MessageId} in chat {ChatId}.", 
                reaction.MessageId, reaction.Chat.Id);
            throw;
        }
    }
}
