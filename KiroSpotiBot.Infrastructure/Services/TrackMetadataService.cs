using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Services;

/// <summary>
/// Service for fetching and storing track metadata with normalization.
/// </summary>
public class TrackMetadataService : ITrackMetadataService
{
    private readonly ISpotifyService _spotifyService;
    private readonly ITrackRepository _trackRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly IGenreRepository _genreRepository;
    private readonly ITrackGenreRepository _trackGenreRepository;
    private readonly ILogger<TrackMetadataService> _logger;

    public TrackMetadataService(
        ISpotifyService spotifyService,
        ITrackRepository trackRepository,
        IArtistRepository artistRepository,
        IAlbumRepository albumRepository,
        IGenreRepository genreRepository,
        ITrackGenreRepository trackGenreRepository,
        ILogger<TrackMetadataService> logger)
    {
        _spotifyService = spotifyService;
        _trackRepository = trackRepository;
        _artistRepository = artistRepository;
        _albumRepository = albumRepository;
        _genreRepository = genreRepository;
        _trackGenreRepository = trackGenreRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SpotifyTrackMetadata?> FetchAndStoreTrackMetadataAsync(string trackId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch track metadata from Spotify.
            var metadata = await _spotifyService.GetTrackAsync(trackId, cancellationToken);
            if (metadata == null)
            {
                _logger.LogWarning("Track {TrackId} not found on Spotify.", trackId);
                return null;
            }

            // Store normalized artist metadata.
            await _artistRepository.GetOrCreateAsync(
                metadata.ArtistSpotifyId,
                metadata.ArtistName,
                cancellationToken);

            // Store normalized album metadata.
            await _albumRepository.GetOrCreateAsync(
                metadata.AlbumSpotifyId,
                metadata.AlbumName,
                metadata.AlbumImageUrl,
                cancellationToken);

            // Store normalized track metadata.
            await _trackRepository.GetOrCreateAsync(trackId, metadata, cancellationToken);

            // Store genre metadata and track-genre relationships.
            foreach (var genre in metadata.Genres)
            {
                await _genreRepository.GetOrCreateAsync(genre, cancellationToken);
                
                try
                {
                    await _trackGenreRepository.CreateAsync(trackId, genre, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Track-genre relationship might already exist, which is fine.
                    _logger.LogDebug(ex, "Track-genre relationship for {TrackId} and {Genre} may already exist.", trackId, genre);
                }
            }

            _logger.LogInformation("Successfully stored metadata for track {TrackId}.", trackId);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and storing metadata for track {TrackId}.", trackId);
            throw;
        }
    }
}
