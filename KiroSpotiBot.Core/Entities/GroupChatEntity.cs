using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents a Telegram group chat configuration in Azure Table Storage.
/// PartitionKey: "GROUPCHAT"
/// RowKey: TelegramChatId as string
/// </summary>
public class GroupChatEntity : MyTableEntity
{
    // Strongly-typed wrapper for RowKey
    [IgnoreDataMember]
    public long TelegramChatId
    {
        get => long.TryParse(RowKey, out var id) ? id : 0;
        set => RowKey = value.ToString();
    }

    // Business properties
    public required string AdministratorTelegramUserId { get; set; }
    public string? PlaylistId { get; set; }
    public string? PlaylistName { get; set; }
    public int DownvoteThreshold { get; set; } = 3;
    public DateTime CreatedAt { get; set; }

    public GroupChatEntity()
    {
        PartitionKey = "GROUPCHAT";
        CreatedAt = DateTime.UtcNow;
    }

    public GroupChatEntity(long telegramChatId, long administratorTelegramUserId) : this()
    {
        TelegramChatId = telegramChatId;
        AdministratorTelegramUserId = administratorTelegramUserId.ToString();
    }
}
