using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for GroupChatMember operations.
/// </summary>
public class GroupChatMemberRepository : BaseRepository<GroupChatMemberEntity>, IGroupChatMemberRepository
{
    public GroupChatMemberRepository(
        TableServiceClient tableServiceClient,
        ILogger<BaseRepository<GroupChatMemberEntity>> logger)
        : base(tableServiceClient, "GroupChatMembers", logger)
    {
    }

    public async Task<IEnumerable<GroupChatMemberEntity>> GetMembersByGroupChatAsync(long telegramChatId, CancellationToken cancellationToken = default)
    {
        return await GetByPartitionKeyAsync(telegramChatId.ToString(), cancellationToken);
    }

    public new async Task<GroupChatMemberEntity> UpsertAsync(GroupChatMemberEntity member, CancellationToken cancellationToken = default)
    {
        return await base.UpsertAsync(member, cancellationToken);
    }
}
