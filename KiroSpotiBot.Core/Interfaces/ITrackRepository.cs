using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Repository for managing normalized track metadata.
/// </summary>
public interface ITrackRepository
{
    /// <summary>
    /// Gets a track by its Spotify ID, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="spotifyId">The Spotify track identifier.</param>
    /// <param name="metadata">The track metadata to use if creating a new track.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The track entity.</returns>
    Task<TrackEntity> GetOrCreateAsync(string spotifyId, SpotifyTrackMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a track by its Spotify ID.
    /// </summary>
    /// <param name="spotifyId">The Spotify track identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The track entity, or null if not found.</returns>
    Task<TrackEntity?> GetAsync(string spotifyId, CancellationToken cancellationToken = default);
}
