namespace KiroSpotiBot.Core.Models;

/// <summary>
/// Represents a user who has contributed tracks to a playlist.
/// </summary>
public class Contributor
{
    public long TelegramUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int TrackCount { get; set; }
}
