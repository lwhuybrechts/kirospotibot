using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
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
}
