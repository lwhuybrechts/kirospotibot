using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository for managing track-genre relationships.
/// </summary>
public class TrackGenreRepository : BaseRepository<TrackGenreEntity>, ITrackGenreRepository
{
    private readonly TableClient _tableClient;

    public TrackGenreRepository(
        TableServiceClient tableServiceClient,
        ILogger<TrackGenreRepository> logger)
        : base(tableServiceClient, "TrackGenres", logger)
    {
        _tableClient = tableServiceClient.GetTableClient("TrackGenres");
    }

    /// <inheritdoc/>
    public async Task<TrackGenreEntity> CreateAsync(string trackSpotifyId, string genreName, CancellationToken cancellationToken = default)
    {
        var entity = new TrackGenreEntity(trackSpotifyId, genreName);
        return await CreateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetGenresForTrackAsync(string trackSpotifyId, CancellationToken cancellationToken = default)
    {
        var genres = new List<string>();
        
        await foreach (var entity in _tableClient.QueryAsync<TrackGenreEntity>(
            filter: $"PartitionKey eq '{trackSpotifyId}'",
            cancellationToken: cancellationToken))
        {
            genres.Add(entity.GenreName);
        }

        return genres;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<GenreInfo>> GetGenresForTracksAsync(IEnumerable<string> trackSpotifyIds, CancellationToken cancellationToken = default)
    {
        var genreCounts = new Dictionary<string, int>();
        
        foreach (var trackSpotifyId in trackSpotifyIds)
        {
            var genres = await GetGenresForTrackAsync(trackSpotifyId, cancellationToken);
            foreach (var genre in genres)
            {
                if (genreCounts.ContainsKey(genre))
                {
                    genreCounts[genre]++;
                }
                else
                {
                    genreCounts[genre] = 1;
                }
            }
        }

        return genreCounts
            .Select(kvp => new GenreInfo { GenreName = kvp.Key, TrackCount = kvp.Value })
            .OrderByDescending(g => g.TrackCount)
            .ToList();
    }
}
