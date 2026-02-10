namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for interacting with the Spotify Web API.
/// </summary>
public interface ISpotifyService
{
    /// <summary>
    /// Retrieves track metadata from Spotify.
    /// </summary>
    /// <param name="trackId">The Spotify track identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full track metadata including name, artist, album, and genres.</returns>
    Task<SpotifyTrackMetadata?> GetTrackAsync(string trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a track to a Spotify playlist.
    /// </summary>
    /// <param name="playlistId">The Spotify playlist identifier.</param>
    /// <param name="trackId">The Spotify track identifier.</param>
    /// <param name="accessToken">The user's Spotify access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the track was added successfully, false otherwise.</returns>
    Task<bool> AddTrackToPlaylistAsync(string playlistId, string trackId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a track from a Spotify playlist.
    /// </summary>
    /// <param name="playlistId">The Spotify playlist identifier.</param>
    /// <param name="trackId">The Spotify track identifier.</param>
    /// <param name="accessToken">The user's Spotify access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the track was removed successfully, false otherwise.</returns>
    Task<bool> RemoveTrackFromPlaylistAsync(string playId, string trackId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a track to the user's Spotify queue.
    /// </summary>
    /// <param name="trackId">The Spotify track identifier.</param>
    /// <param name="accessToken">The user's Spotify access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the track was added to the queue successfully, false otherwise.</returns>
    Task<bool> AddTrackToQueueAsync(string trackId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user is currently playing music on Spotify.
    /// </summary>
    /// <param name="accessToken">The user's Spotify access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user is currently playing music, false otherwise.</returns>
    Task<bool> IsUserPlayingAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired Spotify access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The Spotify refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new access token, or null if refresh failed.</returns>
    Task<string?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a playlist exists and is accessible.
    /// </summary>
    /// <param name="playlistId">The Spotify playlist identifier.</param>
    /// <param name="accessToken">The user's Spotify access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the playlist is valid and accessible, false otherwise.</returns>
    Task<bool> ValidatePlaylistAsync(string playlistId, string accessToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents Spotify track metadata.
/// </summary>
public record SpotifyTrackMetadata(
    string SpotifyId,
    string Name,
    int DurationSeconds,
    string? PreviewUrl,
    string ArtistSpotifyId,
    string ArtistName,
    string AlbumSpotifyId,
    string AlbumName,
    string? AlbumImageUrl,
    IReadOnlyList<string> Genres
);
