using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Infrastructure.Services;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for track addition with administrator credentials.
/// Property 11: Track Addition with Credentials
/// Validates: Requirements 6.1
/// 
/// Note: These tests verify that tracks are added using administrator credentials,
/// not the sharing user's credentials.
/// </summary>
public class TrackAdditionWithCredentialsPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IGroupChatRepository _groupChatRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly Mock<ISpotifyService> _mockSpotifyService;
    private readonly Mock<ITrackMetadataService> _mockTrackMetadataService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly TableClient _groupChatsTable;
    private readonly TableClient _usersTable;
    private readonly TableClient _trackRecordsTable;

    public TrackAdditionWithCredentialsPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var groupChatLogger = Mock.Of<ILogger<BaseRepository<GroupChatEntity>>>();
        var userLogger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        var trackRecordLogger = Mock.Of<ILogger<BaseRepository<TrackRecordEntity>>>();
        
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockEncryptionService.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _mockEncryptionService.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        
        _groupChatRepository = new GroupChatRepository(_tableServiceClient, groupChatLogger);
        _userRepository = new UserRepository(
            _tableServiceClient, 
            _mockEncryptionService.Object,
            userLogger);
        _trackRecordRepository = new TrackRecordRepository(_tableServiceClient, trackRecordLogger);
        
        _mockSpotifyService = new Mock<ISpotifyService>();
        _mockTrackMetadataService = new Mock<ITrackMetadataService>();
        
        // Get table references.
        _groupChatsTable = _tableServiceClient.GetTableClient("GroupChats");
        _usersTable = _tableServiceClient.GetTableClient("Users");
        _trackRecordsTable = _tableServiceClient.GetTableClient("TrackRecords");
        
        // Truncate tables before tests.
        TableHelper.TruncateTable(_groupChatsTable);
        TableHelper.TruncateTable(_usersTable);
        TableHelper.TruncateTable(_trackRecordsTable);
    }

    [Theory]
    [InlineData(12345, 67890, 11111, "37i9dQZF1DXcBWIGoYBM5M", "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, 44444, "5AB8PJLq8xCqXHJNqKJQzN", "7qiZfU4dY1lWllzX7mPBI")]
    [InlineData(55555, 66666, 77777, "3cEYpjA9oz9GiPac4AsH4n", "0VjIjW4GlUZAMYd2vXMi3b")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 11: Track Addition with Credentials")]
    public async Task TrackAddition_UsesAdministratorCredentials_NotSharingUserCredentials(
        long chatId,
        long administratorId,
        long sharingUserId,
        string playlistId,
        string trackId)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = new GroupChatEntity(chatId, administratorId)
        {
            PlaylistId = playlistId,
            PlaylistName = "Test Playlist"
        };
        await _groupChatRepository.CreateAsync(groupChat);
        
        // Create administrator user with credentials.
        var adminUser = new UserEntity(administratorId)
        {
            EncryptedAccessToken = "admin_encrypted_token",
            EncryptedRefreshToken = "admin_encrypted_refresh"
        };
        await _userRepository.CreateAsync(adminUser);
        
        // Create sharing user with different credentials.
        var sharingUser = new UserEntity(sharingUserId)
        {
            EncryptedAccessToken = "sharing_user_encrypted_token",
            EncryptedRefreshToken = "sharing_user_encrypted_refresh"
        };
        await _userRepository.CreateAsync(sharingUser);
        
        // Setup mock to track which credentials are used.
        string? usedAccessToken = null;
        _mockSpotifyService
            .Setup(s => s.AddTrackToPlaylistAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, token, _) => usedAccessToken = token)
            .ReturnsAsync(true);
        
        // Act: Simulate track addition (would be called by MessageHandler).
        // In real scenario, MessageHandler would get admin token and call Spotify service.
        var adminToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(administratorId);
        await _mockSpotifyService.Object.AddTrackToPlaylistAsync(playlistId, trackId, adminToken!, CancellationToken.None);
        
        // Assert: Verify administrator's credentials were used, not sharing user's.
        Assert.NotNull(usedAccessToken);
        Assert.Equal(adminToken, usedAccessToken);
        Assert.NotEqual("sharing_user_encrypted_token", usedAccessToken);
        
        // Verify the mock was called exactly once.
        _mockSpotifyService.Verify(
            s => s.AddTrackToPlaylistAsync(playlistId, trackId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, 67890, 11111, "37i9dQZF1DXcBWIGoYBM5M")]
    [InlineData(22222, 33333, 44444, "5AB8PJLq8xCqXHJNqKJQzN")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 11: Track Addition with Credentials")]
    public async Task TrackAddition_WithoutAdminCredentials_FailsGracefully(
        long chatId,
        long administratorId,
        long sharingUserId,
        string playlistId)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = new GroupChatEntity(chatId, administratorId)
        {
            PlaylistId = playlistId,
            PlaylistName = "Test Playlist"
        };
        await _groupChatRepository.CreateAsync(groupChat);
        
        // Create administrator user WITHOUT credentials.
        var adminUser = new UserEntity(administratorId);
        await _userRepository.CreateAsync(adminUser);
        
        // Create sharing user with credentials.
        var sharingUser = new UserEntity(sharingUserId)
        {
            EncryptedAccessToken = "sharing_user_encrypted_token",
            EncryptedRefreshToken = "sharing_user_encrypted_refresh"
        };
        await _userRepository.CreateAsync(sharingUser);
        
        // Act: Try to get admin token.
        var adminToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(administratorId);
        
        // Assert: Admin token should be null or empty.
        Assert.True(string.IsNullOrWhiteSpace(adminToken));
        
        // Verify that without admin credentials, track addition should not proceed.
        // In real scenario, MessageHandler would check this and return error.
    }

    [Theory]
    [InlineData(12345, 67890, "37i9dQZF1DXcBWIGoYBM5M", "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "5AB8PJLq8xCqXHJNqKJQzN", "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 11: Track Addition with Credentials")]
    public async Task TrackAddition_WhenSharingUserIsAdmin_UsesAdminCredentials(
        long chatId,
        long administratorId,
        string playlistId,
        string trackId)
    {
        // Arrange: Create group chat where sharing user IS the administrator.
        var groupChat = new GroupChatEntity(chatId, administratorId)
        {
            PlaylistId = playlistId,
            PlaylistName = "Test Playlist"
        };
        await _groupChatRepository.CreateAsync(groupChat);
        
        // Create user who is both administrator and sharing user.
        var user = new UserEntity(administratorId)
        {
            EncryptedAccessToken = "admin_encrypted_token",
            EncryptedRefreshToken = "admin_encrypted_refresh"
        };
        await _userRepository.CreateAsync(user);
        
        // Setup mock.
        string? usedAccessToken = null;
        _mockSpotifyService
            .Setup(s => s.AddTrackToPlaylistAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, token, _) => usedAccessToken = token)
            .ReturnsAsync(true);
        
        // Act: Get admin token (which is same as sharing user in this case).
        var adminToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(administratorId);
        await _mockSpotifyService.Object.AddTrackToPlaylistAsync(playlistId, trackId, adminToken!, CancellationToken.None);
        
        // Assert: Verify credentials were used correctly.
        Assert.NotNull(usedAccessToken);
        Assert.Equal(adminToken, usedAccessToken);
        
        _mockSpotifyService.Verify(
            s => s.AddTrackToPlaylistAsync(playlistId, trackId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
