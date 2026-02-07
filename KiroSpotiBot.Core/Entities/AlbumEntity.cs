using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents normalized Spotify album metadata in Azure Table Storage.
/// PartitionKey: "ALBUM"
/// RowKey: SpotifyId
/// </summary>
public class AlbumEntity : MyTableEntity
{
    // Strongly-typed wrapper for RowKey
    [IgnoreDataMember]
    public string SpotifyId
    {
        get => RowKey;
        set => RowKey = value;
    }

    // Business properties
    public required string Name { get; set; }
    public string? ImageUrl { get; set; }

    public AlbumEntity()
    {
        PartitionKey = "ALBUM";
    }

    public AlbumEntity(string spotifyId, string name) : this()
    {
        SpotifyId = spotifyId;
        Name = name;
    }
}
