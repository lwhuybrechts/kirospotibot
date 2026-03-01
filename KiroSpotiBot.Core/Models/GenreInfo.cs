namespace KiroSpotiBot.Core.Models;

/// <summary>
/// Represents genre information with track count.
/// </summary>
public class GenreInfo
{
    public string GenreName { get; set; } = string.Empty;
    public int TrackCount { get; set; }
}
