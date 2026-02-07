using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents normalized Spotify artist metadata in Azure Table Storage.
/// PartitionKey: "ARTIST"
/// RowKey: SpotifyId
/// </summary>
public class ArtistEntity : MyTableEntity
{
    // Strongly-typed wrapper for RowKey
    [IgnoreDataMember]
    public string SpotifyId
    {
        get => RowKey;
        set => RowKey = value;
    }

    // Business properties
    public string Name { get; set; } = string.Empty;

    public ArtistEntity()
    {
        PartitionKey = "ARTIST";
    }

    public ArtistEntity(string spotifyId, string name) : this()
    {
        SpotifyId = spotifyId;
        Name = name;
    }
}
