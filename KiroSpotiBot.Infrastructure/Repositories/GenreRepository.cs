using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository for managing genre metadata.
/// </summary>
public class GenreRepository : BaseRepository<GenreEntity>, IGenreRepository
{
    public GenreRepository(
        TableServiceClient tableServiceClient,
        ILogger<GenreRepository> logger)
        : base(tableServiceClient, "Genres", logger)
    {
    }

    /// <inheritdoc/>
    public async Task<GenreEntity> GetOrCreateAsync(string genreName, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync("GENRE", genreName, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var genre = new GenreEntity(genreName);
        return await CreateAsync(genre, cancellationToken);
    }
}
