namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service interface for calculating user statistics across repositories.
/// </summary>
public interface IUserStatisticsService
{
    /// <summary>
    /// Gets detailed statistics for a specific user.
    /// </summary>
    Task<UserDetailStatistics?> GetUserDetailStatisticsAsync(long telegramUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Detailed user statistics for detail view.
/// </summary>
public class UserDetailStatistics
{
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int TotalTracksShared { get; set; }
    public int TotalUpvotesGiven { get; set; }
    public int TotalDownvotesGiven { get; set; }
    public int TotalUpvotesReceived { get; set; }
    public int TotalDownvotesReceived { get; set; }
    public List<PlaylistStatistics> PlaylistStats { get; set; } = new();
    public List<VoterInfo> Upvoters { get; set; } = new();
    public List<VoterInfo> Downvoters { get; set; } = new();
}

/// <summary>
/// Per-playlist statistics for a user.
/// </summary>
public class PlaylistStatistics
{
    public long TelegramChatId { get; set; }
    public string PlaylistName { get; set; } = string.Empty;
    public int TracksShared { get; set; }
    public int UpvotesGiven { get; set; }
    public int DownvotesGiven { get; set; }
    public int UpvotesReceived { get; set; }
    public int DownvotesReceived { get; set; }
}

/// <summary>
/// Information about a user who voted on tracks.
/// </summary>
public class VoterInfo
{
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int VoteCount { get; set; }
}
