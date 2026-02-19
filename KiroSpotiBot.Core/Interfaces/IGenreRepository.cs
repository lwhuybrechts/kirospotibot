using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Repository for managing genre metadata.
/// </summary>
public interface IGenreRepository
{
    /// <summary>
    /// Gets a genre by name, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="genreName">The genre name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The genre entity.</returns>
    Task<GenreEntity> GetOrCreateAsync(string genreName, CancellationToken cancellationToken = default);
}
