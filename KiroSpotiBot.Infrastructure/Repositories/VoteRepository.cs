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

    public async Task<int> GetTotalUpvotesGivenByUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        // Query all votes where RowKey (TelegramUserId) matches and VoteType is Upvote.
        var filter = $"RowKey eq '{telegramUserId}' and VoteType eq 'Upvote'";
        var votes = await QueryAsync(filter, cancellationToken);
        return votes.Count();
    }

    public async Task<int> GetTotalDownvotesGivenByUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        // Query all votes where RowKey (TelegramUserId) matches and VoteType is Downvote.
        var filter = $"RowKey eq '{telegramUserId}' and VoteType eq 'Downvote'";
        var votes = await QueryAsync(filter, cancellationToken);
        return votes.Count();
    }

    public async Task<int> GetTotalUpvotesReceivedByUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        // This requires joining with TrackRecords to find tracks shared by the user.
        // For now, we'll need to implement this in a service layer that has access to both repositories.
        // Return 0 as placeholder - will be calculated in the service layer.
        return await Task.FromResult(0);
    }

    public async Task<int> GetTotalDownvotesReceivedByUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        // This requires joining with TrackRecords to find tracks shared by the user.
        // For now, we'll need to implement this in a service layer that has access to both repositories.
        // Return 0 as placeholder - will be calculated in the service layer.
        return await Task.FromResult(0);
    }
}
