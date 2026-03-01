namespace KiroSpotiBot.ApiModels;

/// <summary>
/// Data transfer object for user summary information.
/// </summary>
public class UserSummaryDto
{
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int TotalTracksShared { get; set; }
    public int TotalUpvotesGiven { get; set; }
    public int TotalDownvotesGiven { get; set; }
    public int TotalUpvotesReceived { get; set; }
    public int TotalDownvotesReceived { get; set; }
}

/// <summary>
/// Data transfer object for detailed user information.
/// </summary>
public class UserDetailsDto
{
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int TotalTracksShared { get; set; }
    public int TotalUpvotesGiven { get; set; }
    public int TotalDownvotesGiven { get; set; }
    public int TotalUpvotesReceived { get; set; }
    public int TotalDownvotesReceived { get; set; }
    public List<PlaylistStatisticsDto> PlaylistStatistics { get; set; } = new();
    public List<SharedTrackDto> SharedTracks { get; set; } = new();
    public List<VoterDto> VotersOnUserTracks { get; set; } = new();
}

/// <summary>
/// Data transfer object for per-playlist user statistics.
/// </summary>
public class PlaylistStatisticsDto
{
    public long ChatId { get; set; }
    public string PlaylistName { get; set; } = string.Empty;
    public int TracksShared { get; set; }
    public int UpvotesGiven { get; set; }
    public int DownvotesGiven { get; set; }
    public int UpvotesReceived { get; set; }
    public int DownvotesReceived { get; set; }
}

/// <summary>
/// Data transfer object for shared track information.
/// </summary>
public class SharedTrackDto
{
    public string TrackRecordId { get; set; } = string.Empty;
    public string TrackSpotifyId { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public string? AlbumImageUrl { get; set; }
    public long ChatId { get; set; }
    public string PlaylistName { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
}

/// <summary>
/// Data transfer object for voter information.
/// </summary>
public class VoterDto
{
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
}
