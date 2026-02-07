using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents a track sharing event in a group chat in Azure Table Storage.
/// PartitionKey: TelegramChatId as string
/// RowKey: Guid for uniqueness
/// </summary>
public class TrackRecordEntity : MyTableEntity
{
    // Strongly-typed wrapper for PartitionKey
    [IgnoreDataMember]
    public long TelegramChatId
    {
        get => long.TryParse(PartitionKey, out var id) ? id : 0;
        set => PartitionKey = value.ToString();
    }

    [IgnoreDataMember]
    public string TrackRecordId
    {
        get => RowKey;
        set => RowKey = value;
    }

    // Business properties
    public string TrackSpotifyId { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty; // Denormalized for display
    public string ArtistName { get; set; } = string.Empty; // Denormalized for display
    public string AlbumName { get; set; } = string.Empty; // Denormalized for display
    public string? AlbumImageUrl { get; set; } // Denormalized for display
    public long SharedByTelegramUserId { get; set; }
    public string SharedByUsername { get; set; } = string.Empty; // Denormalized for display
    public string? SharedByAvatarUrl { get; set; } // Denormalized for display
    public int TelegramMessageId { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsDuplicate { get; set; }
    public DateTime SharedAt { get; set; }
    public int UpvoteCount { get; set; } // Denormalized for performance
    public int DownvoteCount { get; set; } // Denormalized for performance

    public TrackRecordEntity()
    {
        TrackRecordId = Guid.NewGuid().ToString();
        SharedAt = DateTime.UtcNow;
    }

    public TrackRecordEntity(long telegramChatId, string trackSpotifyId, long sharedByTelegramUserId) : this()
    {
        TelegramChatId = telegramChatId;
        TrackSpotifyId = trackSpotifyId;
        SharedByTelegramUserId = sharedByTelegramUserId;
    }
}
