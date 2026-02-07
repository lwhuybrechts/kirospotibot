using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents normalized Spotify track metadata in Azure Table Storage.
/// PartitionKey: "TRACK"
/// RowKey: SpotifyId
/// </summary>
public class TrackEntity : MyTableEntity
{
    // Strongly-typed wrapper for RowKey.
    [IgnoreDataMember]
    public string SpotifyId
    {
        get => RowKey;
        set => RowKey = value;
    }

    // Business properties.
    public string Name { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string? PreviewUrl { get; set; }
    public string ArtistSpotifyId { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty; // Denormalized for display.
    public string AlbumSpotifyId { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty; // Denormalized for display.
    public string? AlbumImageUrl { get; set; } // Denormalized for display.
    public DateTime CreatedAt { get; set; }

    public TrackEntity()
    {
        PartitionKey = "TRACK";
        CreatedAt = DateTime.UtcNow;
    }

    public TrackEntity(string spotifyId) : this()
    {
        SpotifyId = spotifyId;
    }
}
