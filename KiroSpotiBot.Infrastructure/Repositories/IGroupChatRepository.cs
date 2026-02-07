using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository interface for GroupChat operations.
/// </summary>
public interface IGroupChatRepository : IRepository<GroupChatEntity>
{
    /// <summary>
    /// Gets a group chat by Telegram chat ID.
    /// </summary>
    Task<GroupChatEntity?> GetByTelegramChatIdAsync(long telegramChatId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a playlist is already linked to any group chat.
    /// </summary>
    Task<bool> IsPlaylistLinkedAsync(string playlistId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new group chat entity.
    /// </summary>
    Task<GroupChatEntity> CreateGroupChatAsync(long telegramChatId, long administratorTelegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing group chat entity.
    /// </summary>
    Task<GroupChatEntity> UpdateGroupChatAsync(GroupChatEntity groupChat, CancellationToken cancellationToken = default);
}
