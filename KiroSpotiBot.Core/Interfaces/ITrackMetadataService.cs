namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for fetching and storing track metadata with normalization.
/// </summary>
public interface ITrackMetadataService
{
    /// <summary>
    /// Fetches track metadata from Spotify and stores it in normalized tables.
    /// </summary>
    /// <param name="trackId">The Spotify track identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Spotify track metadata, or null if the track was not found.</returns>
    Task<SpotifyTrackMetadata?> FetchAndStoreTrackMetadataAsync(string trackId, CancellationToken cancellationToken = default);
}
