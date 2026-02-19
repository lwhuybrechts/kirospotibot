using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Vote operations.
/// </summary>
public interface IVoteRepository : IRepository<VoteEntity>
{
    /// <summary>
    /// Upserts a vote (creates or updates).
    /// </summary>
    Task<VoteEntity> UpsertVoteAsync(VoteEntity vote, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a vote.
    /// </summary>
    Task DeleteVoteAsync(string trackRecordId, long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all votes for a track record.
    /// </summary>
    Task<IEnumerable<VoteEntity>> GetByTrackRecordAsync(string trackRecordId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific vote by track record and user.
    /// </summary>
    Task<VoteEntity?> GetVoteAsync(string trackRecordId, long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of upvotes for a track record.
    /// </summary>
    Task<int> GetUpvoteCountAsync(string trackRecordId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of downvotes for a track record.
    /// </summary>
    Task<int> GetDownvoteCountAsync(string trackRecordId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets vote counts (upvotes and downvotes) for a track record.
    /// </summary>
    Task<(int upvotes, int downvotes)> GetVoteCountsAsync(string trackRecordId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets total upvotes given by a user across all tracks.
    /// </summary>
    Task<int> GetTotalUpvotesGivenByUserAsync(long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets total downvotes given by a user across all tracks.
    /// </summary>
    Task<int> GetTotalDownvotesGivenByUserAsync(long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets total upvotes received by a user on their shared tracks.
    /// </summary>
    Task<int> GetTotalUpvotesReceivedByUserAsync(long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets total downvotes received by a user on their shared tracks.
    /// </summary>
    Task<int> GetTotalDownvotesReceivedByUserAsync(long telegramUserId, CancellationToken cancellationToken = default);
}
