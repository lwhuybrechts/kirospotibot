using Telegram.Bot.Types;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for handling Telegram bot commands.
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Handles the /auth command to initiate Spotify authentication.
    /// </summary>
    /// <param name="message">The Telegram message containing the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAuthCommandAsync(Message message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Handles the /configure command to set the playlist for a group chat.
    /// </summary>
    /// <param name="message">The Telegram message containing the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleConfigureCommandAsync(Message message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Handles the /threshold command to set the downvote threshold.
    /// </summary>
    /// <param name="message">The Telegram message containing the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleThresholdCommandAsync(Message message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Handles the /autoqueue command to enable/disable auto-queue.
    /// </summary>
    /// <param name="message">The Telegram message containing the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAutoQueueCommandAsync(Message message, CancellationToken cancellationToken);
}
