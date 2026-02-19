using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Repository interface for UserGroupConfig operations.
/// </summary>
public interface IUserGroupConfigRepository
{
    /// <summary>
    /// Gets the user group configuration for a specific user in a specific group.
    /// </summary>
    /// <param name="telegramChatId">The Telegram chat ID.</param>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user group configuration, or null if not found.</returns>
    Task<UserGroupConfigEntity?> GetAsync(long telegramChatId, long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates or updates a user group configuration.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved configuration.</returns>
    Task<UserGroupConfigEntity> UpsertAsync(UserGroupConfigEntity config, CancellationToken cancellationToken = default);
}
