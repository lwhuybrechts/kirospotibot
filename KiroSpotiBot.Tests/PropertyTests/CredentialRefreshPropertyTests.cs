using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Infrastructure.Services;
using KiroSpotiBot.Infrastructure.Options;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for credential persistence and refresh validation.
/// Property 7: Credential Persistence and Refresh
/// Validates: Requirements 4.5
/// 
/// Note: These tests use xUnit's Theory attribute with InlineData to simulate
/// property-based testing behavior by testing multiple input combinations.
/// The Users table is truncated before each test class instantiation to ensure clean state.
/// </summary>
public class CredentialRefreshPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IUserRepository _userRepository;
    private readonly ISpotifyService _spotifyService;
    private readonly IEncryptionService _encryptionService;
    private readonly TableClient _tableClient;

    public CredentialRefreshPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        // Connection string for local development storage.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        // Set up encryption service.
        var encryptionOptions = Options.Create(new EncryptionOptions
        {
            Key = "TestKey12345678901234567890123456" // 32 characters for AES-256.
        });
        _encryptionService = new AesEncryptionService(encryptionOptions);
        
        // Set up user repository.
        var userLogger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        _userRepository = new UserRepository(_tableServiceClient, _encryptionService, userLogger);
        
        // Set up Spotify service with mock options.
        var spotifyOptions = Options.Create(new SpotifyOptions
        {
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret"
        });
        var spotifyLogger = Mock.Of<ILogger<SpotifyService>>();
        _spotifyService = new SpotifyService(spotifyLogger, spotifyOptions);
        
        // Get reference to the Users table.
        _tableClient = _tableServiceClient.GetTableClient("Users");
        
        // Truncate the table before tests to ensure clean state.
        TableHelper.TruncateTable(_tableClient);
    }

    [Theory]
    [InlineData(12345, "initial_access_token", "refresh_token_abc", 3600, "playlist-modify-public")]
    [InlineData(67890, "access_xyz", "refresh_xyz", 7200, "playlist-modify-public playlist-modify-private")]
    [InlineData(11111, "token_123", "refresh_123", 1800, "user-modify-playback-state")]
    [InlineData(22222, "old_token", "refresh_old", 3600, "playlist-modify-public user-modify-playback-state")]
    [InlineData(33333, "expired_token", "refresh_valid", 600, "playlist-modify-public playlist-modify-private user-modify-playback-state")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 7: Credential Persistence and Refresh")]
    public async Task CredentialPersistence_StoreAndRetrieve_ReturnsDecryptedCredentials(
        long telegramUserId,
        string accessToken,
        string refreshToken,
        int expiresIn,
        string scope)
    {
        // Arrange & Act: Store encrypted credentials.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            accessToken,
            refreshToken,
            expiresIn,
            scope);
        
        // Retrieve decrypted credentials.
        var retrievedAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(telegramUserId);
        var retrievedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        
        // Assert: Verify credentials match original values.
        Assert.NotNull(retrievedAccessToken);
        Assert.NotNull(retrievedRefreshToken);
        Assert.Equal(accessToken, retrievedAccessToken);
        Assert.Equal(refreshToken, retrievedRefreshToken);
    }

    [Theory]
    [InlineData(12345, "initial_access", "refresh_abc", 3600, "scope1", "new_access", "refresh_abc", 3600, "scope1")]
    [InlineData(67890, "old_access", "old_refresh", 7200, "scope2", "refreshed_access", "old_refresh", 7200, "scope2")]
    [InlineData(11111, "expired_token", "refresh_123", 1800, "scope3", "new_token_123", "refresh_123", 1800, "scope3")]
    [InlineData(22222, "token_a", "refresh_a", 3600, "scope4", "token_b", "refresh_a", 3600, "scope4")]
    [InlineData(33333, "access_old", "refresh_valid", 600, "scope5", "access_new", "refresh_valid", 600, "scope5")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 7: Credential Persistence and Refresh")]
    public async Task CredentialRefresh_UpdateAccessToken_PersistsNewCredentials(
        long telegramUserId,
        string initialAccessToken,
        string refreshToken,
        int expiresIn,
        string scope,
        string newAccessToken,
        string expectedRefreshToken,
        int expectedExpiresIn,
        string expectedScope)
    {
        // Arrange: Store initial credentials.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            initialAccessToken,
            refreshToken,
            expiresIn,
            scope);
        
        // Act: Simulate credential refresh by updating with new access token.
        // In a real scenario, this would be triggered by SpotifyService.RefreshAccessTokenAsync.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            newAccessToken,
            expectedRefreshToken,
            expectedExpiresIn,
            expectedScope);
        
        // Retrieve updated credentials.
        var retrievedAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(telegramUserId);
        var retrievedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId);
        
        // Assert: Verify new access token is persisted.
        Assert.NotNull(retrievedAccessToken);
        Assert.NotNull(retrievedRefreshToken);
        Assert.NotNull(user);
        Assert.Equal(newAccessToken, retrievedAccessToken);
        Assert.Equal(expectedRefreshToken, retrievedRefreshToken);
        Assert.Equal(expectedExpiresIn, user.TokenExpiresIn);
        Assert.Equal(expectedScope, user.Scope);
        
        // Verify old access token is no longer retrievable.
        Assert.NotEqual(initialAccessToken, retrievedAccessToken);
    }

    [Theory]
    [InlineData(12345, "access_token_1", "refresh_token_1", 3600, "scope1")]
    [InlineData(67890, "access_token_2", "refresh_token_2", 7200, "scope2")]
    [InlineData(11111, "access_token_3", "refresh_token_3", 1800, "scope3")]
    [InlineData(22222, "access_token_4", "refresh_token_4", 3600, "scope4")]
    [InlineData(33333, "access_token_5", "refresh_token_5", 600, "scope5")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 7: Credential Persistence and Refresh")]
    public async Task CredentialEncryption_StoredCredentials_AreNotPlaintext(
        long telegramUserId,
        string accessToken,
        string refreshToken,
        int expiresIn,
        string scope)
    {
        // Arrange & Act: Store credentials.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            accessToken,
            refreshToken,
            expiresIn,
            scope);
        
        // Retrieve the raw entity from storage.
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId);
        
        // Assert: Verify stored credentials are encrypted (not plaintext).
        Assert.NotNull(user);
        Assert.NotNull(user.EncryptedAccessToken);
        Assert.NotNull(user.EncryptedRefreshToken);
        
        // Encrypted values should not match plaintext.
        Assert.NotEqual(accessToken, user.EncryptedAccessToken);
        Assert.NotEqual(refreshToken, user.EncryptedRefreshToken);
        
        // Verify decryption returns original values.
        var decryptedAccess = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(telegramUserId);
        var decryptedRefresh = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        Assert.Equal(accessToken, decryptedAccess);
        Assert.Equal(refreshToken, decryptedRefresh);
    }

    [Theory]
    [InlineData(12345, "access_1", "refresh_1", 3600, "scope1", "access_2", "refresh_1", 3600, "scope1", "access_3", "refresh_1", 3600, "scope1")]
    [InlineData(67890, "token_a", "refresh_a", 7200, "scope2", "token_b", "refresh_a", 7200, "scope2", "token_c", "refresh_a", 7200, "scope2")]
    [InlineData(11111, "old_1", "refresh_x", 1800, "scope3", "old_2", "refresh_x", 1800, "scope3", "new_3", "refresh_x", 1800, "scope3")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 7: Credential Persistence and Refresh")]
    public async Task CredentialRefresh_MultipleRefreshes_PersistsLatestCredentials(
        long telegramUserId,
        string accessToken1,
        string refreshToken1,
        int expiresIn1,
        string scope1,
        string accessToken2,
        string refreshToken2,
        int expiresIn2,
        string scope2,
        string accessToken3,
        string refreshToken3,
        int expiresIn3,
        string scope3)
    {
        // Arrange: Store initial credentials.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            accessToken1,
            refreshToken1,
            expiresIn1,
            scope1);
        
        // Act: Perform first refresh.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            accessToken2,
            refreshToken2,
            expiresIn2,
            scope2);
        
        // Perform second refresh.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            accessToken3,
            refreshToken3,
            expiresIn3,
            scope3);
        
        // Retrieve final credentials.
        var finalAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(telegramUserId);
        var finalRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId);
        
        // Assert: Verify only the latest credentials are stored.
        Assert.NotNull(finalAccessToken);
        Assert.NotNull(finalRefreshToken);
        Assert.NotNull(user);
        Assert.Equal(accessToken3, finalAccessToken);
        Assert.Equal(refreshToken3, finalRefreshToken);
        Assert.Equal(expiresIn3, user.TokenExpiresIn);
        Assert.Equal(scope3, user.Scope);
        
        // Verify previous tokens are not retrievable.
        Assert.NotEqual(accessToken1, finalAccessToken);
        Assert.NotEqual(accessToken2, finalAccessToken);
    }

    [Theory]
    [InlineData(12345)]
    [InlineData(67890)]
    [InlineData(11111)]
    [InlineData(22222)]
    [InlineData(33333)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 7: Credential Persistence and Refresh")]
    public async Task CredentialRetrieval_NonExistentUser_ReturnsNull(long telegramUserId)
    {
        // Act: Attempt to retrieve credentials for non-existent user.
        var accessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(telegramUserId);
        var refreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        
        // Assert: Verify null is returned.
        Assert.Null(accessToken);
        Assert.Null(refreshToken);
    }

    [Theory]
    [InlineData(12345, "access_token", "refresh_token", 3600, "playlist-modify-public")]
    [InlineData(67890, "token_xyz", "refresh_xyz", 7200, "playlist-modify-public playlist-modify-private")]
    [InlineData(11111, "test_token", "test_refresh", 1800, "user-modify-playback-state")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 7: Credential Persistence and Refresh")]
    public async Task CredentialPersistence_ScopeAndExpiration_ArePersisted(
        long telegramUserId,
        string accessToken,
        string refreshToken,
        int expiresIn,
        string scope)
    {
        // Arrange & Act: Store credentials with scope and expiration.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            accessToken,
            refreshToken,
            expiresIn,
            scope);
        
        // Retrieve user entity.
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId);
        
        // Assert: Verify scope and expiration are persisted.
        Assert.NotNull(user);
        Assert.Equal(expiresIn, user.TokenExpiresIn);
        Assert.Equal(scope, user.Scope);
    }

    [Theory]
    [InlineData(12345, "expired_access_token", "valid_refresh_token", 3600, "playlist-modify-public")]
    [InlineData(67890, "old_access", "refresh_abc", 7200, "playlist-modify-public playlist-modify-private")]
    [InlineData(11111, "stale_token", "refresh_xyz", 1800, "user-modify-playback-state")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 7: Credential Persistence and Refresh")]
    public async Task CredentialRefresh_WithSpotifyService_AttemptsRefresh(
        long telegramUserId,
        string expiredAccessToken,
        string refreshToken,
        int expiresIn,
        string scope)
    {
        // Arrange: Store initial credentials with expired access token.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            expiredAccessToken,
            refreshToken,
            expiresIn,
            scope);
        
        // Retrieve the refresh token.
        var storedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        
        // Act: Attempt to refresh the access token using SpotifyService.
        // Note: This will return null because we're not using real Spotify credentials.
        // The test validates that the refresh flow can be invoked without errors.
        var newAccessToken = await _spotifyService.RefreshAccessTokenAsync(storedRefreshToken!);
        
        // Assert: Verify the refresh was attempted (returns null for invalid tokens).
        // In a real scenario with valid tokens, this would return a new access token.
        Assert.Null(newAccessToken); // Expected for test credentials.
        
        // Verify original credentials are still retrievable.
        var retrievedAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(telegramUserId);
        var retrievedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        Assert.Equal(expiredAccessToken, retrievedAccessToken);
        Assert.Equal(refreshToken, retrievedRefreshToken);
    }

    [Theory]
    [InlineData(12345, "expired_token", "refresh_token_1", 3600, "scope1", "new_access_token", 3600, "scope1")]
    [InlineData(67890, "old_token", "refresh_token_2", 7200, "scope2", "refreshed_token", 7200, "scope2")]
    [InlineData(11111, "stale_token", "refresh_token_3", 1800, "scope3", "fresh_token", 1800, "scope3")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 7: Credential Persistence and Refresh")]
    public async Task CredentialRefresh_SimulatedRefreshFlow_PersistsNewToken(
        long telegramUserId,
        string expiredAccessToken,
        string refreshToken,
        int expiresIn,
        string scope,
        string simulatedNewAccessToken,
        int newExpiresIn,
        string newScope)
    {
        // Arrange: Store initial credentials with expired access token.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            expiredAccessToken,
            refreshToken,
            expiresIn,
            scope);
        
        // Retrieve the refresh token.
        var storedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        Assert.NotNull(storedRefreshToken);
        
        // Act: Simulate the refresh flow.
        // Step 1: Attempt refresh (would return new token in real scenario).
        var newAccessToken = await _spotifyService.RefreshAccessTokenAsync(storedRefreshToken);
        
        // Step 2: If refresh succeeded (simulated here), update credentials.
        // In real code, this would only happen if newAccessToken is not null.
        if (newAccessToken == null)
        {
            // Simulate successful refresh by using our test token.
            newAccessToken = simulatedNewAccessToken;
        }
        
        // Step 3: Persist the new access token.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            telegramUserId,
            newAccessToken,
            storedRefreshToken,
            newExpiresIn,
            newScope);
        
        // Assert: Verify new credentials are persisted.
        var retrievedAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(telegramUserId);
        var retrievedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(telegramUserId);
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId);
        
        Assert.NotNull(retrievedAccessToken);
        Assert.NotNull(retrievedRefreshToken);
        Assert.NotNull(user);
        Assert.Equal(simulatedNewAccessToken, retrievedAccessToken);
        Assert.Equal(refreshToken, retrievedRefreshToken);
        Assert.Equal(newExpiresIn, user.TokenExpiresIn);
        Assert.Equal(newScope, user.Scope);
        
        // Verify old access token is no longer retrievable.
        Assert.NotEqual(expiredAccessToken, retrievedAccessToken);
    }
}
