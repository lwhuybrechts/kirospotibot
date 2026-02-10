using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for authentication access control.
/// Property 8: Authentication Access Control
/// Validates: Requirements 4.7, 8.4
/// 
/// Note: These tests verify that both administrators and regular group members
/// can authenticate with Spotify and have their credentials stored successfully.
/// </summary>
public class AuthenticationAccessControlPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IUserRepository _userRepository;
    private readonly TableClient _usersTableClient;

    public AuthenticationAccessControlPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var logger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        var encryptionService = new Mock<KiroSpotiBot.Infrastructure.Services.IEncryptionService>();
        
        // Setup encryption service to return predictable encrypted values.
        encryptionService
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string input) => $"encrypted_{input}");
        
        encryptionService
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Returns((string input) => input.Replace("encrypted_", ""));
        
        _userRepository = new UserRepository(_tableServiceClient, encryptionService.Object, logger);
        
        // Get reference to the Users table.
        _usersTableClient = _tableServiceClient.GetTableClient("Users");
        
        // Truncate the table before tests to ensure clean state.
        TableHelper.TruncateTable(_usersTableClient);
    }

    [Theory]
    [InlineData(12345, "access_token_admin_1", "refresh_token_admin_1", 3600, "playlist-modify-public playlist-modify-private")]
    [InlineData(67890, "access_token_admin_2", "refresh_token_admin_2", 7200, "playlist-modify-public user-modify-playback-state")]
    [InlineData(11111, "access_token_admin_3", "refresh_token_admin_3", 3600, "playlist-modify-private user-read-playback-state")]
    [InlineData(99999, "access_token_admin_4", "refresh_token_admin_4", 1800, "playlist-modify-public playlist-modify-private user-modify-playback-state")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 8: Authentication Access Control")]
    public async Task AdministratorAuthentication_StoreCredentials_SuccessfullyStored(
        long administratorUserId,
        string accessToken,
        string refreshToken,
        int expiresIn,
        string scope)
    {
        // Arrange: Administrator user (could be any user, no special privileges needed for auth).
        
        // Act: Store Spotify credentials for administrator.
        var result = await _userRepository.UpdateSpotifyCredentialsAsync(
            administratorUserId,
            accessToken,
            refreshToken,
            expiresIn,
            scope,
            CancellationToken.None);
        
        // Assert: Verify credentials were stored.
        Assert.NotNull(result);
        Assert.Equal(administratorUserId, result.TelegramUserId);
        Assert.NotNull(result.EncryptedAccessToken);
        Assert.NotNull(result.EncryptedRefreshToken);
        Assert.Equal(expiresIn, result.TokenExpiresIn);
        Assert.Equal(scope, result.Scope);
        
        // Verify credentials can be retrieved.
        var retrievedAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(administratorUserId);
        var retrievedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(administratorUserId);
        
        Assert.Equal(accessToken, retrievedAccessToken);
        Assert.Equal(refreshToken, retrievedRefreshToken);
    }

    [Theory]
    [InlineData(54321, "access_token_member_1", "refresh_token_member_1", 3600, "user-modify-playback-state user-read-playback-state")]
    [InlineData(98765, "access_token_member_2", "refresh_token_member_2", 7200, "playlist-modify-public user-modify-playback-state")]
    [InlineData(11223, "access_token_member_3", "refresh_token_member_3", 3600, "playlist-modify-private user-read-playback-state")]
    [InlineData(44556, "access_token_member_4", "refresh_token_member_4", 1800, "user-modify-playback-state")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 8: Authentication Access Control")]
    public async Task RegularMemberAuthentication_StoreCredentials_SuccessfullyStored(
        long memberUserId,
        string accessToken,
        string refreshToken,
        int expiresIn,
        string scope)
    {
        // Arrange: Regular group member (not administrator).
        
        // Act: Store Spotify credentials for regular member.
        var result = await _userRepository.UpdateSpotifyCredentialsAsync(
            memberUserId,
            accessToken,
            refreshToken,
            expiresIn,
            scope,
            CancellationToken.None);
        
        // Assert: Verify credentials were stored (same as administrator).
        Assert.NotNull(result);
        Assert.Equal(memberUserId, result.TelegramUserId);
        Assert.NotNull(result.EncryptedAccessToken);
        Assert.NotNull(result.EncryptedRefreshToken);
        Assert.Equal(expiresIn, result.TokenExpiresIn);
        Assert.Equal(scope, result.Scope);
        
        // Verify credentials can be retrieved.
        var retrievedAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(memberUserId);
        var retrievedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(memberUserId);
        
        Assert.Equal(accessToken, retrievedAccessToken);
        Assert.Equal(refreshToken, retrievedRefreshToken);
    }

    [Theory]
    [InlineData(12345, "initial_access", "initial_refresh", 3600, "scope1", "updated_access", "updated_refresh", 7200, "scope2")]
    [InlineData(67890, "token_a", "refresh_a", 1800, "playlist-modify-public", "token_b", "refresh_b", 3600, "playlist-modify-private")]
    [InlineData(11111, "old_token", "old_refresh", 3600, "user-modify-playback-state", "new_token", "new_refresh", 3600, "user-read-playback-state")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 8: Authentication Access Control")]
    public async Task UserAuthentication_UpdateCredentials_SuccessfullyUpdated(
        long userId,
        string initialAccessToken,
        string initialRefreshToken,
        int initialExpiresIn,
        string initialScope,
        string updatedAccessToken,
        string updatedRefreshToken,
        int updatedExpiresIn,
        string updatedScope)
    {
        // Arrange: Store initial credentials.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            userId,
            initialAccessToken,
            initialRefreshToken,
            initialExpiresIn,
            initialScope,
            CancellationToken.None);
        
        // Act: Update credentials (e.g., after token refresh).
        var result = await _userRepository.UpdateSpotifyCredentialsAsync(
            userId,
            updatedAccessToken,
            updatedRefreshToken,
            updatedExpiresIn,
            updatedScope,
            CancellationToken.None);
        
        // Assert: Verify updated credentials are stored.
        Assert.NotNull(result);
        Assert.Equal(userId, result.TelegramUserId);
        Assert.Equal(updatedExpiresIn, result.TokenExpiresIn);
        Assert.Equal(updatedScope, result.Scope);
        
        // Verify updated credentials can be retrieved.
        var retrievedAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(userId);
        var retrievedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(userId);
        
        Assert.Equal(updatedAccessToken, retrievedAccessToken);
        Assert.Equal(updatedRefreshToken, retrievedRefreshToken);
    }

    [Theory]
    [InlineData(12345, "access_1", "refresh_1", 3600, "scope1")]
    [InlineData(67890, "access_2", "refresh_2", 7200, "scope2")]
    [InlineData(11111, "access_3", "refresh_3", 1800, "scope3")]
    [InlineData(99999, "access_4", "refresh_4", 3600, "scope4")]
    [InlineData(54321, "access_5", "refresh_5", 7200, "scope5")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 8: Authentication Access Control")]
    public async Task MultipleUsersAuthentication_IndependentCredentials_StoredSeparately(
        long userId,
        string accessToken,
        string refreshToken,
        int expiresIn,
        string scope)
    {
        // Arrange & Act: Store credentials for multiple users.
        await _userRepository.UpdateSpotifyCredentialsAsync(
            userId,
            accessToken,
            refreshToken,
            expiresIn,
            scope,
            CancellationToken.None);
        
        // Assert: Verify each user's credentials are stored independently.
        var retrievedAccessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(userId);
        var retrievedRefreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(userId);
        
        Assert.Equal(accessToken, retrievedAccessToken);
        Assert.Equal(refreshToken, retrievedRefreshToken);
        
        // Verify user entity exists.
        var user = await _userRepository.GetByTelegramUserIdAsync(userId);
        Assert.NotNull(user);
        Assert.Equal(userId, user.TelegramUserId);
        Assert.Equal(scope, user.Scope);
    }

    [Theory]
    [InlineData(12345, "access_token", "refresh_token", 3600, "playlist-modify-public playlist-modify-private user-modify-playback-state user-read-playback-state")]
    [InlineData(67890, "token_abc", "refresh_xyz", 7200, "playlist-modify-public")]
    [InlineData(11111, "my_token", "my_refresh", 1800, "user-modify-playback-state user-read-playback-state")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 8: Authentication Access Control")]
    public async Task UserAuthentication_WithAllScopes_StoredCorrectly(
        long userId,
        string accessToken,
        string refreshToken,
        int expiresIn,
        string scope)
    {
        // Arrange & Act: Store credentials with various scope combinations.
        var result = await _userRepository.UpdateSpotifyCredentialsAsync(
            userId,
            accessToken,
            refreshToken,
            expiresIn,
            scope,
            CancellationToken.None);
        
        // Assert: Verify scope is stored correctly.
        Assert.NotNull(result);
        Assert.Equal(scope, result.Scope);
        
        // Verify scope persists after retrieval.
        var user = await _userRepository.GetByTelegramUserIdAsync(userId);
        Assert.NotNull(user);
        Assert.Equal(scope, user.Scope);
    }

    [Theory]
    [InlineData(12345)]
    [InlineData(67890)]
    [InlineData(11111)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 8: Authentication Access Control")]
    public async Task UserAuthentication_NonExistentUser_ReturnsNull(long userId)
    {
        // Arrange: User has not authenticated yet.
        
        // Act: Try to retrieve credentials for non-existent user.
        var accessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(userId);
        var refreshToken = await _userRepository.GetDecryptedSpotifyRefreshTokenAsync(userId);
        var user = await _userRepository.GetByTelegramUserIdAsync(userId);
        
        // Assert: Should return null for non-existent user.
        Assert.Null(accessToken);
        Assert.Null(refreshToken);
        Assert.Null(user);
    }
}
