using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents per-user, per-group configuration in Azure Table Storage.
/// PartitionKey: TelegramChatId as string
/// RowKey: TelegramUserId as string
/// </summary>
public class UserGroupConfigEntity : MyTableEntity
{
    // Strongly-typed wrappers
    [IgnoreDataMember]
    public long TelegramChatId
    {
        get => long.TryParse(PartitionKey, out var id) ? id : 0;
        set => PartitionKey = value.ToString();
    }

    [IgnoreDataMember]
    public long TelegramUserId
    {
        get => long.TryParse(RowKey, out var id) ? id : 0;
        set => RowKey = value.ToString();
    }

    // Business properties
    public bool AutoQueueEnabled { get; set; } = false;

    public UserGroupConfigEntity()
    {
    }

    public UserGroupConfigEntity(long telegramChatId, long telegramUserId)
    {
        TelegramChatId = telegramChatId;
        TelegramUserId = telegramUserId;
    }
}
