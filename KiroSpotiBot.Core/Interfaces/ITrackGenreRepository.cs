using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Repository for managing track-genre relationships.
/// </summary>
public interface ITrackGenreRepository
{
    /// <summary>
    /// Creates a track-genre relationship.
    /// </summary>
    /// <param name="trackSpotifyId">The Spotify track identifier.</param>
    /// <param name="genreName">The genre name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The track-genre entity.</returns>
    Task<TrackGenreEntity> CreateAsync(string trackSpotifyId, string genreName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all genres for a track.
    /// </summary>
    /// <param name="trackSpotifyId">The Spotify track identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of genre names.</returns>
    Task<IEnumerable<string>> GetGenresForTrackAsync(string trackSpotifyId, CancellationToken cancellationToken = default);
}
