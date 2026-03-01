namespace KiroSpotiBot.ApiModels;

/// <summary>
/// Data transfer object for playlist information.
/// </summary>
public class PlaylistDto
{
    public long ChatId { get; set; }
    public string PlaylistId { get; set; } = string.Empty;
    public string PlaylistName { get; set; } = string.Empty;
    public int DownvoteThreshold { get; set; }
    public long AdministratorTelegramUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
