using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents an OAuth state parameter for CSRF protection in Azure Table Storage.
/// PartitionKey: "OAUTHSTATE"
/// RowKey: State parameter (GUID)
/// </summary>
public class OAuthStateEntity : MyTableEntity
{
    // Strongly-typed wrapper for RowKey.
    [IgnoreDataMember]
    public string State
    {
        get => RowKey;
        set => RowKey = value;
    }

    // Business properties.
    public long TelegramUserId { get; set; }
    public long TelegramChatId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public OAuthStateEntity()
    {
        PartitionKey = "OAUTHSTATE";
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(10); // OAuth state expires in 10 minutes.
    }

    public OAuthStateEntity(string state, long telegramUserId, long telegramChatId) : this()
    {
        State = state;
        TelegramUserId = telegramUserId;
        TelegramChatId = telegramChatId;
    }
}
