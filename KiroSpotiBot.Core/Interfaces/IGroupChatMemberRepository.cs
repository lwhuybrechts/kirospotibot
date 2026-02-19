using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Repository for managing group chat membership.
/// </summary>
public interface IGroupChatMemberRepository
{
    /// <summary>
    /// Gets all members of a group chat.
    /// </summary>
    /// <param name="telegramChatId">The Telegram chat identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of group chat members.</returns>
    Task<IEnumerable<GroupChatMemberEntity>> GetMembersByGroupChatAsync(long telegramChatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a group chat member.
    /// </summary>
    /// <param name="member">The group chat member entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated member entity.</returns>
    Task<GroupChatMemberEntity> UpsertAsync(GroupChatMemberEntity member, CancellationToken cancellationToken = default);
}
