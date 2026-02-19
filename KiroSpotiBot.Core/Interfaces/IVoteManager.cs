namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for managing track voting logic and automatic removal.
/// </summary>
public interface IVoteManager
{
    /// <summary>
    /// Records a vote (upvote or downvote) for a track.
    /// </summary>
    /// <param name="trackRecordId">The track record identifier.</param>
    /// <param name="telegramChatId">The Telegram chat identifier.</param>
    /// <param name="telegramUserId">The user's Telegram identifier.</param>
    /// <param name="voteType">The vote type ("Upvote" or "Downvote").</param>
    /// <param name="voterUsername">The voter's username for denormalization.</param>
    /// <param name="voterAvatarUrl">The voter's avatar URL for denormalization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the track was removed due to reaching threshold, false otherwise.</returns>
    Task<bool> RecordVoteAsync(
        string trackRecordId,
        long telegramChatId,
        long telegramUserId,
        string voteType,
        string voterUsername,
        string? voterAvatarUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user's vote from a track.
    /// </summary>
    /// <param name="trackRecordId">The track record identifier.</param>
    /// <param name="telegramChatId">The Telegram chat identifier.</param>
    /// <param name="telegramUserId">The user's Telegram identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveVoteAsync(
        string trackRecordId,
        long telegramChatId,
        long telegramUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the vote counts for a track.
    /// </summary>
    /// <param name="trackRecordId">The track record identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing upvote and downvote counts.</returns>
    Task<(int upvotes, int downvotes)> GetVoteCountsAsync(
        string trackRecordId,
        CancellationToken cancellationToken = default);
}
