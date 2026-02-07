using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for GroupChat operations.
/// </summary>
public class GroupChatRepository : BaseRepository<GroupChatEntity>, IGroupChatRepository
{
    public GroupChatRepository(
        TableServiceClient tableServiceClient, 
        ILogger<BaseRepository<GroupChatEntity>> logger) 
        : base(tableServiceClient, "GroupChats", logger)
    {
    }

    public async Task<GroupChatEntity?> GetByTelegramChatIdAsync(long telegramChatId, CancellationToken cancellationToken = default)
    {
        return await GetAsync("GROUPCHAT", telegramChatId.ToString(), cancellationToken);
    }

    public async Task<bool> IsPlaylistLinkedAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            return false;
        }

        // Query all group chats to check if playlist is already linked.
        var filter = $"PlaylistId eq '{playlistId}'";
        var results = await QueryAsync(filter, cancellationToken);
        return results.Any();
    }

    public async Task<GroupChatEntity> CreateGroupChatAsync(long telegramChatId, long administratorTelegramUserId, CancellationToken cancellationToken = default)
    {
        var entity = new GroupChatEntity(telegramChatId, administratorTelegramUserId);
        return await CreateAsync(entity, cancellationToken);
    }

    public async Task<GroupChatEntity> UpdateGroupChatAsync(GroupChatEntity groupChat, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(groupChat, cancellationToken);
    }
}
