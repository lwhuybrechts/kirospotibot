using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents group membership for efficient queries in Azure Table Storage.
/// PartitionKey: TelegramChatId as string
/// RowKey: TelegramUserId as string
/// </summary>
public class GroupChatMemberEntity : MyTableEntity
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
    public DateTime JoinedAt { get; set; }

    public GroupChatMemberEntity()
    {
        JoinedAt = DateTime.UtcNow;
    }

    public GroupChatMemberEntity(long telegramChatId, long telegramUserId) : this()
    {
        TelegramChatId = telegramChatId;
        TelegramUserId = telegramUserId;
    }
}
