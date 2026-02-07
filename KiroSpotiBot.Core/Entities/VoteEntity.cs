using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents a user vote on a track in Azure Table Storage.
/// PartitionKey: TrackRecordId (RowKey from TrackRecords)
/// RowKey: TelegramUserId as string
/// </summary>
public class VoteEntity : MyTableEntity
{
    // Strongly-typed wrappers
    [IgnoreDataMember]
    public string TrackRecordId
    {
        get => PartitionKey;
        set => PartitionKey = value;
    }

    [IgnoreDataMember]
    public long TelegramUserId
    {
        get => long.TryParse(RowKey, out var id) ? id : 0;
        set => RowKey = value.ToString();
    }

    // Business properties
    public string VoteType { get; set; } = string.Empty; // "Upvote" or "Downvote"
    public string VoterUsername { get; set; } = string.Empty; // Denormalized for display
    public string? VoterAvatarUrl { get; set; } // Denormalized for display
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public VoteEntity()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public VoteEntity(string trackRecordId, long telegramUserId, string voteType) : this()
    {
        TrackRecordId = trackRecordId;
        TelegramUserId = telegramUserId;
        VoteType = voteType;
    }
}
