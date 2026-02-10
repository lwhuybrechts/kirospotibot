using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Repository interface for OAuth state operations.
/// </summary>
public interface IOAuthStateRepository
{
    /// <summary>
    /// Creates a new OAuth state entity.
    /// </summary>
    Task<OAuthStateEntity> CreateAsync(OAuthStateEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an OAuth state entity by state parameter.
    /// </summary>
    Task<OAuthStateEntity?> GetByStateAsync(string state, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an OAuth state entity.
    /// </summary>
    Task DeleteAsync(string state, CancellationToken cancellationToken = default);
}
