using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents a Telegram user with Spotify authentication in Azure Table Storage.
/// PartitionKey: "USER"
/// RowKey: TelegramUserId as string
/// </summary>
public class UserEntity : MyTableEntity
{
    // Strongly-typed wrapper for RowKey.
    [IgnoreDataMember]
    public long TelegramUserId
    {
        get => long.TryParse(RowKey, out var id) ? id : 0;
        set => RowKey = value.ToString();
    }

    // Business properties.
    public string? EncryptedAccessToken { get; set; }
    public string? EncryptedRefreshToken { get; set; }
    public int? TokenExpiresIn { get; set; }
    public string? Scope { get; set; }
    public string? TelegramAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity()
    {
        PartitionKey = "USER";
        CreatedAt = DateTime.UtcNow;
    }

    public UserEntity(long telegramUserId) : this()
    {
        TelegramUserId = telegramUserId;
    }
}
