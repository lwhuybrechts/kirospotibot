using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Repository for managing normalized album metadata.
/// </summary>
public interface IAlbumRepository
{
    /// <summary>
    /// Gets an album by Spotify ID, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="spotifyId">The Spotify album identifier.</param>
    /// <param name="name">The album name.</param>
    /// <param name="imageUrl">The album image URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The album entity.</returns>
    Task<AlbumEntity> GetOrCreateAsync(string spotifyId, string name, string? imageUrl, CancellationToken cancellationToken = default);
}
