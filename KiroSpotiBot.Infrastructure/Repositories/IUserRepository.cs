using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository interface for User operations.
/// </summary>
public interface IUserRepository : IRepository<UserEntity>
{
    /// <summary>
    /// Gets a user by Telegram user ID.
    /// </summary>
    Task<UserEntity?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new user entity.
    /// </summary>
    Task<UserEntity> CreateUserAsync(long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates user Spotify credentials with encryption.
    /// </summary>
    Task<UserEntity> UpdateSpotifyCredentialsAsync(
        long telegramUserId, 
        string spotifyAccessToken, 
        string spotifyRefreshToken, 
        int expiresIn, 
        string scope, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets decrypted Spotify access token for a user.
    /// </summary>
    Task<string?> GetDecryptedSpotifyAccessTokenAsync(long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets decrypted Spotify refresh token for a user.
    /// </summary>
    Task<string?> GetDecryptedSpotifyRefreshTokenAsync(long telegramUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing user entity.
    /// </summary>
    Task<UserEntity> UpdateUserAsync(UserEntity user, CancellationToken cancellationToken = default);
}
