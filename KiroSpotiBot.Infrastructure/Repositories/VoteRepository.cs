using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Vote operations with counting methods.
/// </summary>
public class VoteRepository : BaseRepository<VoteEntity>, IVoteRepository
{
    public VoteRepository(
        TableServiceClient tableServiceClient,
        ILogger<BaseRepository<VoteEntity>> logger)
        : base(tableServiceClient, "Votes", logger)
    {
    }

    public async Task<VoteEntity> UpsertVoteAsync(VoteEntity vote, CancellationToken cancellationToken = default)
    {
        vote.UpdatedAt = DateTime.UtcNow;
        return await UpsertAsync(vote, cancellationToken);
    }

    public async Task DeleteVoteAsync(string trackRecordId, long telegramUserId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(trackRecordId, telegramUserId.ToString(), cancellationToken);
    }

    public async Task<IEnumerable<VoteEntity>> GetByTrackRecordAsync(string trackRecordId, CancellationToken cancellationToken = default)
    {
        return await GetByPartitionKeyAsync(trackRecordId, cancellationToken);
    }

    public async Task<VoteEntity?> GetVoteAsync(string trackRecordId, long telegramUserId, CancellationToken cancellationToken = default)
    {
        return await GetAsync(trackRecordId, telegramUserId.ToString(), cancellationToken);
    }

    public async Task<int> GetUpvoteCountAsync(string trackRecordId, CancellationToken cancellationToken = default)
    {
        var votes = await GetByTrackRecordAsync(trackRecordId, cancellationToken);
        return votes.Count(v => v.VoteType == "Upvote");
    }

    public async Task<int> GetDownvoteCountAsync(string trackRecordId, CancellationToken cancellationToken = default)
    {
        var votes = await GetByTrackRecordAsync(trackRecordId, cancellationToken);
        return votes.Count(v => v.VoteType == "Downvote");
    }

    public async Task<(int upvotes, int downvotes)> GetVoteCountsAsync(string trackRecordId, CancellationToken cancellationToken = default)
    {
        var votes = await GetByTrackRecordAsync(trackRecordId, cancellationToken);
        var votesList = votes.ToList();
        
        var upvotes = votesList.Count(v => v.VoteType == "Upvote");
        var downvotes = votesList.Count(v => v.VoteType == "Downvote");
        
        return (upvotes, downvotes);
    }
}
