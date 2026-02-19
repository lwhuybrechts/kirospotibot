using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository for managing normalized track metadata.
/// </summary>
public class TrackRepository : BaseRepository<TrackEntity>, ITrackRepository
{
    public TrackRepository(
        TableServiceClient tableServiceClient,
        ILogger<TrackRepository> logger)
        : base(tableServiceClient, "Tracks", logger)
    {
    }

    /// <inheritdoc/>
    public async Task<TrackEntity> GetOrCreateAsync(string spotifyId, SpotifyTrackMetadata metadata, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync("TRACK", spotifyId, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var track = new TrackEntity(spotifyId)
        {
            Name = metadata.Name,
            DurationSeconds = metadata.DurationSeconds,
            PreviewUrl = metadata.PreviewUrl,
            ArtistSpotifyId = metadata.ArtistSpotifyId,
            ArtistName = metadata.ArtistName,
            AlbumSpotifyId = metadata.AlbumSpotifyId,
            AlbumName = metadata.AlbumName,
            AlbumImageUrl = metadata.AlbumImageUrl
        };

        return await CreateAsync(track, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TrackEntity?> GetAsync(string spotifyId, CancellationToken cancellationToken = default)
    {
        return await GetAsync("TRACK", spotifyId, cancellationToken);
    }
}
