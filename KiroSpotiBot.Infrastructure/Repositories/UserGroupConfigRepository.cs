using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserGroupConfig operations.
/// </summary>
public class UserGroupConfigRepository : BaseRepository<UserGroupConfigEntity>, IUserGroupConfigRepository
{
    public UserGroupConfigRepository(
        TableServiceClient tableServiceClient,
        ILogger<BaseRepository<UserGroupConfigEntity>> logger)
        : base(tableServiceClient, "UserGroupConfigs", logger)
    {
    }

    public async Task<UserGroupConfigEntity?> GetAsync(long telegramChatId, long telegramUserId, CancellationToken cancellationToken = default)
    {
        return await GetAsync(telegramChatId.ToString(), telegramUserId.ToString(), cancellationToken);
    }

    public new async Task<UserGroupConfigEntity> UpsertAsync(UserGroupConfigEntity config, CancellationToken cancellationToken = default)
    {
        return await base.UpsertAsync(config, cancellationToken);
    }

    public async Task<IEnumerable<UserGroupConfigEntity>> GetUsersWithAutoQueueEnabledAsync(long telegramChatId, CancellationToken cancellationToken = default)
    {
        var partitionKey = telegramChatId.ToString();
        var filter = $"PartitionKey eq '{partitionKey}' and AutoQueueEnabled eq true";
        return await QueryAsync(filter, cancellationToken);
    }
}
