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

    public MessageHandler(
        ILogger<MessageHandler> logger,
        ITelegramBotClient telegramBotClient,
        IUserRepository userRepository,
        IGroupChatRepository groupChatRepository,
        ISpotifyUrlDetector spotifyUrlDetector,
        IGroupConfigurationValidator configurationValidator,
        ITrackAdditionHandler trackAdditionHandler,
        IGroupSetupHandler groupSetupHandler)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _userRepository = userRepository;
        _groupChatRepository = groupChatRepository;
        _spotifyUrlDetector = spotifyUrlDetector;
        _configurationValidator = configurationValidator;
        _trackAdditionHandler = trackAdditionHandler;
        _groupSetupHandler = groupSetupHandler;
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
}
