using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository for managing normalized artist metadata.
/// </summary>
public class ArtistRepository : BaseRepository<ArtistEntity>, IArtistRepository
{
    public ArtistRepository(
        TableServiceClient tableServiceClient,
        ILogger<ArtistRepository> logger)
        : base(tableServiceClient, "Artists", logger)
    {
    }

    /// <inheritdoc/>
    public async Task<ArtistEntity> GetOrCreateAsync(string spotifyId, string name, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync("ARTIST", spotifyId, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var artist = new ArtistEntity(spotifyId, name);
        return await CreateAsync(artist, cancellationToken);
    }
}
