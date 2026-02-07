using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents many-to-many relationship between tracks and genres in Azure Table Storage.
/// PartitionKey: TrackSpotifyId
/// RowKey: GenreName
/// </summary>
public class TrackGenreEntity : MyTableEntity
{
    // Strongly-typed wrappers
    [IgnoreDataMember]
    public string TrackSpotifyId
    {
        get => PartitionKey;
        set => PartitionKey = value;
    }

    [IgnoreDataMember]
    public string GenreName
    {
        get => RowKey;
        set => RowKey = value;
    }

    public TrackGenreEntity()
    {
    }

    public TrackGenreEntity(string trackSpotifyId, string genreName)
    {
        TrackSpotifyId = trackSpotifyId;
        GenreName = genreName;
    }
}
