using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for handling track addition to playlists.
/// </summary>
public interface ITrackAdditionHandler
{
    /// <summary>
    /// Processes the addition of a track to the playlist.
    /// </summary>
    /// <param name="trackId">The Spotify track ID.</param>
    /// <param name="groupChat">The group chat entity.</param>
    /// <param name="sharedByUserId">The Telegram user ID who shared the track.</param>
    /// <param name="messageId">The Telegram message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessTrackAdditionAsync(
        string trackId,
        GroupChatEntity groupChat,
        long sharedByUserId,
        int messageId,
        CancellationToken cancellationToken);
}
