using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository interface for TrackRecord operations.
/// </summary>
public interface ITrackRecordRepository : IRepository<TrackRecordEntity>
{
    /// <summary>
    /// Gets track records for a group chat with pagination.
    /// </summary>
    Task<IEnumerable<TrackRecordEntity>> GetByGroupChatAsync(
        long telegramChatId, 
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a track record by its ID.
    /// </summary>
    Task<TrackRecordEntity?> GetByIdAsync(string trackRecordId, long telegramChatId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a track has been deleted in a group chat.
    /// </summary>
    Task<bool> IsTrackDeletedAsync(long telegramChatId, string spotifyTrackId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a track already exists in a group chat (not deleted).
    /// </summary>
    Task<bool> TrackExistsAsync(long telegramChatId, string spotifyTrackId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new track record.
    /// </summary>
    Task<TrackRecordEntity> CreateTrackRecordAsync(TrackRecordEntity trackRecord, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing track record.
    /// </summary>
    Task<TrackRecordEntity> UpdateTrackRecordAsync(TrackRecordEntity trackRecord, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a track as deleted.
    /// </summary>
    Task MarkTrackAsDeletedAsync(string trackRecordId, long telegramChatId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all track records for a specific Spotify track in a group chat.
    /// </summary>
    Task<IEnumerable<TrackRecordEntity>> GetBySpotifyTrackIdAsync(
        long telegramChatId, 
        string spotifyTrackId, 
        CancellationToken cancellationToken = default);
}
