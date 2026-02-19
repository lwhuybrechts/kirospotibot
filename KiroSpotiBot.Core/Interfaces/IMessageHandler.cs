using Telegram.Bot.Types;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for handling Telegram message processing logic.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Handles incoming text messages from Telegram.
    /// </summary>
    /// <param name="message">The Telegram message to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleMessageAsync(Message message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Handles bot being added to a group chat.
    /// </summary>
    /// <param name="update">The chat member update event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleBotAddedToGroupAsync(ChatMemberUpdated update, CancellationToken cancellationToken);
    
    /// <summary>
    /// Handles message reaction updates (upvotes/downvotes).
    /// </summary>
    /// <param name="reaction">The message reaction update event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleMessageReactionAsync(MessageReactionUpdated reaction, CancellationToken cancellationToken);
    
    /// <summary>
    /// Handles callback queries from inline keyboard buttons.
    /// </summary>
    /// <param name="callbackQuery">The callback query event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken);
}
