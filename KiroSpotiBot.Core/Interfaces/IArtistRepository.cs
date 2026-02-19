using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Repository for managing normalized artist metadata.
/// </summary>
public interface IArtistRepository
{
    /// <summary>
    /// Gets an artist by Spotify ID, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="spotifyId">The Spotify artist identifier.</param>
    /// <param name="name">The artist name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The artist entity.</returns>
    Task<ArtistEntity> GetOrCreateAsync(string spotifyId, string name, CancellationToken cancellationToken = default);
}
