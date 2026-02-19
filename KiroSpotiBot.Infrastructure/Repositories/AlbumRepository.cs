using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository for managing normalized album metadata.
/// </summary>
public class AlbumRepository : BaseRepository<AlbumEntity>, IAlbumRepository
{
    public AlbumRepository(
        TableServiceClient tableServiceClient,
        ILogger<AlbumRepository> logger)
        : base(tableServiceClient, "Albums", logger)
    {
    }

    /// <inheritdoc/>
    public async Task<AlbumEntity> GetOrCreateAsync(string spotifyId, string name, string? imageUrl, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync("ALBUM", spotifyId, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var album = new AlbumEntity(spotifyId, name)
        {
            ImageUrl = imageUrl
        };
        return await CreateAsync(album, cancellationToken);
    }
}
