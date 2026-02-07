using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User operations with credential encryption.
/// </summary>
public class UserRepository : BaseRepository<UserEntity>, IUserRepository
{
    private readonly IEncryptionService _encryptionService;

    public UserRepository(
        TableServiceClient tableServiceClient,
        IEncryptionService encryptionService,
        ILogger<BaseRepository<UserEntity>> logger)
        : base(tableServiceClient, "Users", logger)
    {
        _encryptionService = encryptionService;
    }

    public async Task<UserEntity?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        return await GetAsync("USER", telegramUserId.ToString(), cancellationToken);
    }

    public async Task<UserEntity> CreateUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        var entity = new UserEntity(telegramUserId);
        return await CreateAsync(entity, cancellationToken);
    }

    public async Task<UserEntity> UpdateSpotifyCredentialsAsync(
        long telegramUserId,
        string spotifyAccessToken,
        string spotifyRefreshToken,
        int expiresIn,
        string scope,
        CancellationToken cancellationToken = default)
    {
        var user = await GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            user = new UserEntity(telegramUserId);
        }

        // Encrypt Spotify credentials before storing.
        user.EncryptedAccessToken = _encryptionService.Encrypt(spotifyAccessToken);
        user.EncryptedRefreshToken = _encryptionService.Encrypt(spotifyRefreshToken);
        user.TokenExpiresIn = expiresIn;
        user.Scope = scope;

        return await UpsertAsync(user, cancellationToken);
    }

    public async Task<string?> GetDecryptedSpotifyAccessTokenAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user?.EncryptedAccessToken == null)
        {
            return null;
        }

        return _encryptionService.Decrypt(user.EncryptedAccessToken);
    }

    public async Task<string?> GetDecryptedSpotifyRefreshTokenAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user?.EncryptedRefreshToken == null)
        {
            return null;
        }

        return _encryptionService.Decrypt(user.EncryptedRefreshToken);
    }

    public async Task<UserEntity> UpdateUserAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(user, cancellationToken);
    }
}
