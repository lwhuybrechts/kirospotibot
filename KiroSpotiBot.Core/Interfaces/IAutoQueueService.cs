namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service interface for auto-queue operations.
/// </summary>
public interface IAutoQueueService
{
    /// <summary>
    /// Triggers auto-queue for all users with auto-queue enabled in a group chat.
    /// </summary>
    /// <param name="telegramChatId">The Telegram chat ID.</param>
    /// <param name="trackId">The Spotify track ID to add to queues.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task TriggerAutoQueueAsync(long telegramChatId, string trackId, CancellationToken cancellationToken = default);
}
