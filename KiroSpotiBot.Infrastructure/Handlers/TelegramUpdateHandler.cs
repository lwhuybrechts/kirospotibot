using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KiroSpotiBot.Infrastructure.Handlers;

/// <summary>
/// Handles routing and processing of Telegram updates.
/// </summary>
public class TelegramUpdateHandler : ITelegramUpdateHandler
{
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IMessageHandler _messageHandler;

    public TelegramUpdateHandler(
        ILogger<TelegramUpdateHandler> logger,
        ITelegramBotClient telegramBotClient,
        IMessageHandler messageHandler)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _messageHandler = messageHandler;
    }

    /// <summary>
    /// Routes the update to the appropriate handler based on update type.
    /// </summary>
    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing Telegram update of type {UpdateType} with ID {UpdateId}.",
                update.Type, update.Id);

            switch (update.Type)
            {
                case UpdateType.Message:
                    if (update.Message != null)
                    {
                        await HandleMessageAsync(update.Message, cancellationToken);
                    }
                    break;

                case UpdateType.CallbackQuery:
                    if (update.CallbackQuery != null)
                    {
                        await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
                    }
                    break;

                case UpdateType.MessageReaction:
                    if (update.MessageReaction != null)
                    {
                        await HandleMessageReactionAsync(update.MessageReaction, cancellationToken);
                    }
                    break;

                case UpdateType.MyChatMember:
                    if (update.MyChatMember != null)
                    {
                        await HandleChatMemberUpdateAsync(update.MyChatMember, cancellationToken);
                    }
                    break;

                default:
                    _logger.LogDebug("Ignoring update type {UpdateType}.", update.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing update of type {UpdateType}.", update.Type);
            throw;
        }
    }

    /// <summary>
    /// Handles incoming messages.
    /// </summary>
    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing message {MessageId} from chat {ChatId}.",
                message.MessageId, message.Chat.Id);

            // Delegate to MessageHandler for business logic.
            await _messageHandler.HandleMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message {MessageId} from chat {ChatId}.",
                message.MessageId, message.Chat.Id);
            throw;
        }
    }

    /// <summary>
    /// Handles callback queries from inline buttons.
    /// </summary>
    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing callback query {CallbackQueryId} from user {UserId}.",
                callbackQuery.Id, callbackQuery.From.Id);

            // TODO: Implement callback query handling logic in Task 15.
            // For now, just acknowledge the callback.
            await _telegramBotClient.AnswerCallbackQuery(
                callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling callback query {CallbackQueryId}.",
                callbackQuery.Id);
            throw;
        }
    }

    /// <summary>
    /// Handles message reactions (upvotes/downvotes).
    /// </summary>
    private async Task HandleMessageReactionAsync(MessageReactionUpdated reaction, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing message reaction for message {MessageId} in chat {ChatId}.",
                reaction.MessageId, reaction.Chat.Id);

            // Delegate to MessageHandler for business logic.
            await _messageHandler.HandleMessageReactionAsync(reaction, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message reaction for message {MessageId} in chat {ChatId}.",
                reaction.MessageId, reaction.Chat.Id);
            throw;
        }
    }

    /// <summary>
    /// Handles chat member updates (bot added/removed from group).
    /// </summary>
    private async Task HandleChatMemberUpdateAsync(ChatMemberUpdated update, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing chat member update in chat {ChatId}.", update.Chat.Id);

            // Check if bot was added to the group.
            var oldStatus = update.OldChatMember.Status;
            var newStatus = update.NewChatMember.Status;

            if ((oldStatus == ChatMemberStatus.Left || oldStatus == ChatMemberStatus.Kicked) &&
                (newStatus == ChatMemberStatus.Member || newStatus == ChatMemberStatus.Administrator))
            {
                _logger.LogInformation("Bot was added to group {ChatId} by user {UserId}.",
                    update.Chat.Id, update.From.Id);

                // Delegate to MessageHandler for business logic.
                await _messageHandler.HandleBotAddedToGroupAsync(update, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling chat member update in chat {ChatId}.", update.Chat.Id);
            throw;
        }
    }
}
