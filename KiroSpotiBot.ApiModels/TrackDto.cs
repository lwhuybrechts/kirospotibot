namespace KiroSpotiBot.ApiModels;

/// <summary>
/// Data transfer object for track information.
/// </summary>
public class TrackDto
{
    public string TrackRecordId { get; set; } = string.Empty;
    public string TrackSpotifyId { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public string? AlbumImageUrl { get; set; }
    public long SharedByTelegramUserId { get; set; }
    public string SharedByUsername { get; set; } = string.Empty;
    public string? SharedByAvatarUrl { get; set; }
    public DateTime SharedAt { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<VoteDto> Votes { get; set; } = new();
}

/// <summary>
/// Data transfer object for vote information.
/// </summary>
public class VoteDto
{
    public long TelegramUserId { get; set; }
    public string VoteType { get; set; } = string.Empty;
    public string VoterUsername { get; set; } = string.Empty;
    public string? VoterAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Data transfer object for contributor information.
/// </summary>
public class ContributorDto
{
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int TrackCount { get; set; }
}

/// <summary>
/// Data transfer object for genre information.
/// </summary>
public class GenreDto
{
    public string GenreName { get; set; } = string.Empty;
    public int TrackCount { get; set; }
}
