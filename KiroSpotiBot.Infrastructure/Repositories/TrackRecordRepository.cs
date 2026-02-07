using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TrackRecord operations with pagination support.
/// </summary>
public class TrackRecordRepository : BaseRepository<TrackRecordEntity>, ITrackRecordRepository
{
    public TrackRecordRepository(
        TableServiceClient tableServiceClient,
        ILogger<BaseRepository<TrackRecordEntity>> logger)
        : base(tableServiceClient, "TrackRecords", logger)
    {
    }

    public async Task<IEnumerable<TrackRecordEntity>> GetByGroupChatAsync(
        long telegramChatId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var allRecords = await GetByPartitionKeyAsync(telegramChatId.ToString(), cancellationToken);
        
        // Apply pagination and ordering.
        return allRecords
            .OrderByDescending(r => r.SharedAt)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<TrackRecordEntity?> GetByIdAsync(string trackRecordId, long telegramChatId, CancellationToken cancellationToken = default)
    {
        return await GetAsync(telegramChatId.ToString(), trackRecordId, cancellationToken);
    }

    public async Task<bool> IsTrackDeletedAsync(long telegramChatId, string spotifyTrackId, CancellationToken cancellationToken = default)
    {
        var records = await GetBySpotifyTrackIdAsync(telegramChatId, spotifyTrackId, cancellationToken);
        return records.Any(r => r.IsDeleted);
    }

    public async Task<bool> TrackExistsAsync(long telegramChatId, string spotifyTrackId, CancellationToken cancellationToken = default)
    {
        var records = await GetBySpotifyTrackIdAsync(telegramChatId, spotifyTrackId, cancellationToken);
        return records.Any(r => !r.IsDeleted && !r.IsDuplicate);
    }

    public async Task<TrackRecordEntity> CreateTrackRecordAsync(TrackRecordEntity trackRecord, CancellationToken cancellationToken = default)
    {
        return await CreateAsync(trackRecord, cancellationToken);
    }

    public async Task<TrackRecordEntity> UpdateTrackRecordAsync(TrackRecordEntity trackRecord, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(trackRecord, cancellationToken);
    }

    public async Task MarkTrackAsDeletedAsync(string trackRecordId, long telegramChatId, CancellationToken cancellationToken = default)
    {
        var trackRecord = await GetByIdAsync(trackRecordId, telegramChatId, cancellationToken);
        if (trackRecord != null)
        {
            trackRecord.IsDeleted = true;
            await UpdateAsync(trackRecord, cancellationToken);
        }
    }

    public async Task<IEnumerable<TrackRecordEntity>> GetBySpotifyTrackIdAsync(
        long telegramChatId,
        string spotifyTrackId,
        CancellationToken cancellationToken = default)
    {
        var filter = $"PartitionKey eq '{telegramChatId}' and TrackSpotifyId eq '{spotifyTrackId}'";
        return await QueryAsync(filter, cancellationToken);
    }
}
