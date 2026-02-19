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
}
