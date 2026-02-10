using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;

namespace KiroSpotiBot.Infrastructure.Services;

/// <summary>
/// Service for interacting with the Spotify Web API.
/// </summary>
public class SpotifyService : ISpotifyService
{
    private readonly ILogger<SpotifyService> _logger;
    private readonly SpotifyOptions _options;

    public SpotifyService(
        ILogger<SpotifyService> logger,
        IOptions<SpotifyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<SpotifyTrackMetadata?> GetTrackAsync(string trackId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use client credentials flow for public track metadata retrieval.
            var config = SpotifyClientConfig.CreateDefault();
            var request = new ClientCredentialsRequest(_options.ClientId, _options.ClientSecret);
            var response = await new OAuthClient(config).RequestToken(request, cancellationToken);

            var spotify = new SpotifyClient(config.WithToken(response.AccessToken));
            var track = await spotify.Tracks.Get(trackId, cancellationToken);

            if (track == null)
            {
                _logger.LogWarning("Track {TrackId} not found on Spotify.", trackId);
                return null;
            }

            // Get artist genres (track genres are not directly available).
            var artist = await spotify.Artists.Get(track.Artists[0].Id, cancellationToken);
            var genres = artist?.Genres ?? new List<string>();

            return new SpotifyTrackMetadata(
                SpotifyId: track.Id,
                Name: track.Name,
                DurationSeconds: track.DurationMs / 1000,
                PreviewUrl: track.PreviewUrl,
                ArtistSpotifyId: track.Artists[0].Id,
                ArtistName: track.Artists[0].Name,
                AlbumSpotifyId: track.Album.Id,
                AlbumName: track.Album.Name,
                AlbumImageUrl: track.Album.Images.FirstOrDefault()?.Url,
                Genres: genres.AsReadOnly()
            );
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error while retrieving track {TrackId}: {Message}", trackId, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving track {TrackId}.", trackId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AddTrackToPlaylistAsync(string playlistId, string trackId, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var spotify = new SpotifyClient(accessToken);
            var trackUri = $"spotify:track:{trackId}";
            
            var request = new PlaylistAddItemsRequest(new List<string> { trackUri });
            await spotify.Playlists.AddItems(playlistId, request, cancellationToken);

            _logger.LogInformation("Successfully added track {TrackId} to playlist {PlaylistId}.", trackId, playlistId);
            return true;
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Unauthorized access when adding track {TrackId} to playlist {PlaylistId}. Token may be expired.", trackId, playlistId);
            return false;
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error while adding track {TrackId} to playlist {PlaylistId}: {Message}", trackId, playlistId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding track {TrackId} to playlist {PlaylistId}.", trackId, playlistId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveTrackFromPlaylistAsync(string playlistId, string trackId, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var spotify = new SpotifyClient(accessToken);
            var trackUri = $"spotify:track:{trackId}";
            
            var request = new PlaylistRemoveItemsRequest
            {
                Tracks = new List<PlaylistRemoveItemsRequest.Item>
                {
                    new PlaylistRemoveItemsRequest.Item { Uri = trackUri }
                }
            };
            
            await spotify.Playlists.RemoveItems(playlistId, request, cancellationToken);

            _logger.LogInformation("Successfully removed track {TrackId} from playlist {PlaylistId}.", trackId, playlistId);
            return true;
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Unauthorized access when removing track {TrackId} from playlist {PlaylistId}. Token may be expired.", trackId, playlistId);
            return false;
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error while removing track {TrackId} from playlist {PlaylistId}: {Message}", trackId, playlistId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while removing track {TrackId} from playlist {PlaylistId}.", trackId, playlistId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AddTrackToQueueAsync(string trackId, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var spotify = new SpotifyClient(accessToken);
            var trackUri = $"spotify:track:{trackId}";
            
            await spotify.Player.AddToQueue(new PlayerAddToQueueRequest(trackUri), cancellationToken);

            _logger.LogInformation("Successfully added track {TrackId} to user's queue.", trackId);
            return true;
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Unauthorized access when adding track {TrackId} to queue. Token may be expired.", trackId);
            return false;
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("User is not playing music, cannot add track {TrackId} to queue.", trackId);
            return false;
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error while adding track {TrackId} to queue: {Message}", trackId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding track {TrackId} to queue.", trackId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsUserPlayingAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var spotify = new SpotifyClient(accessToken);
            var playback = await spotify.Player.GetCurrentPlayback(cancellationToken);

            return playback?.IsPlaying ?? false;
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Unauthorized access when checking playback state. Token may be expired.");
            return false;
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error while checking playback state: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while checking playback state.");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = SpotifyClientConfig.CreateDefault();
            var request = new AuthorizationCodeRefreshRequest(_options.ClientId, _options.ClientSecret, refreshToken);
            var response = await new OAuthClient(config).RequestToken(request, cancellationToken);

            _logger.LogInformation("Successfully refreshed Spotify access token.");
            return response.AccessToken;
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error while refreshing access token: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while refreshing access token.");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidatePlaylistAsync(string playlistId, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var spotify = new SpotifyClient(accessToken);
            var playlist = await spotify.Playlists.Get(playlistId, cancellationToken);

            return playlist != null;
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Playlist {PlaylistId} not found.", playlistId);
            return false;
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Unauthorized access to playlist {PlaylistId}. Token may be expired.", playlistId);
            return false;
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error while validating playlist {PlaylistId}: {Message}", playlistId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while validating playlist {PlaylistId}.", playlistId);
            return false;
        }
    }
}
