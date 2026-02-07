using System.Runtime.Serialization;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Represents a music genre in Azure Table Storage.
/// PartitionKey: "GENRE"
/// RowKey: Genre name
/// </summary>
public class GenreEntity : MyTableEntity
{
    // Strongly-typed wrapper for RowKey
    [IgnoreDataMember]
    public string GenreName
    {
        get => RowKey;
        set => RowKey = value;
    }

    public GenreEntity()
    {
        PartitionKey = "GENRE";
    }

    public GenreEntity(string genreName) : this()
    {
        GenreName = genreName;
    }
}
