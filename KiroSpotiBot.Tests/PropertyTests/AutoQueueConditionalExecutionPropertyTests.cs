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
/// Property-based tests for auto-queue conditional execution.
/// Property 41: Auto-Queue Conditional Execution
/// Validates: Requirements 19.3, 19.4
/// 
/// Note: These tests verify that tracks are only added to a user's queue
/// when auto-queue is enabled AND the user is currently playing music.
/// </summary>
public class AutoQueueConditionalExecutionPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IUserGroupConfigRepository _userGroupConfigRepository;
    private readonly IUserRepository _userRepository;
    private readonly Mock<ISpotifyService> _mockSpotifyService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly IAutoQueueService _autoQueueService;
    private readonly TableClient _userGroupConfigsTable;
    private readonly TableClient _usersTable;

    public AutoQueueConditionalExecutionPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var userGroupConfigLogger = Mock.Of<ILogger<BaseRepository<UserGroupConfigEntity>>>();
        var userLogger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        var autoQueueLogger = Mock.Of<ILogger<AutoQueueService>>();
        
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockEncryptionService.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _mockEncryptionService.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        
        _userGroupConfigRepository = new UserGroupConfigRepository(_tableServiceClient, userGroupConfigLogger);
        _userRepository = new UserRepository(
            _tableServiceClient, 
            _mockEncryptionService.Object,
            userLogger);
        
        _mockSpotifyService = new Mock<ISpotifyService>();
        
        _autoQueueService = new AutoQueueService(
            autoQueueLogger,
            _userGroupConfigRepository,
            _userRepository,
            _mockSpotifyService.Object);
        
        // Get table references.
        _userGroupConfigsTable = _tableServiceClient.GetTableClient("UserGroupConfigs");
        _usersTable = _tableServiceClient.GetTableClient("Users");
        
        // Truncate tables before tests.
        TableHelper.TruncateTable(_userGroupConfigsTable);
        TableHelper.TruncateTable(_usersTable);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "7qiZfU4dY1lWllzX7mPBI")]
    [InlineData(55555, 66666, "0VjIjW4GlUZAMYd2vXMi3b")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 41: Auto-Queue Conditional Execution")]
    public async Task AutoQueue_WhenEnabledAndUserIsPlaying_AddsTrackToQueue(
        long chatId,
        long userId,
        string trackId)
    {
        // Arrange: Create user with auto-queue enabled.
        var userConfig = new UserGroupConfigEntity(chatId, userId)
        {
            AutoQueueEnabled = true
        };
        await _userGroupConfigRepository.UpsertAsync(userConfig);
        
        // Create user with valid credentials.
        var user = new UserEntity(userId)
        {
            EncryptedAccessToken = "valid_access_token",
            EncryptedRefreshToken = "valid_refresh_token"
        };
        await _userRepository.CreateAsync(user);
        
        // Setup mock: User is currently playing music.
        _mockSpotifyService
            .Setup(s => s.IsUserPlayingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockSpotifyService
            .Setup(s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        // Act: Trigger auto-queue.
        await _autoQueueService.TriggerAutoQueueAsync(chatId, trackId, CancellationToken.None);
        
        // Assert: Verify track was added to queue.
        _mockSpotifyService.Verify(
            s => s.IsUserPlayingAsync("valid_access_token", It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(trackId, "valid_access_token", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 41: Auto-Queue Conditional Execution")]
    public async Task AutoQueue_WhenEnabledButUserNotPlaying_DoesNotAddToQueue(
        long chatId,
        long userId,
        string trackId)
    {
        // Arrange: Create user with auto-queue enabled.
        var userConfig = new UserGroupConfigEntity(chatId, userId)
        {
            AutoQueueEnabled = true
        };
        await _userGroupConfigRepository.UpsertAsync(userConfig);
        
        // Create user with valid credentials.
        var user = new UserEntity(userId)
        {
            EncryptedAccessToken = "valid_access_token",
            EncryptedRefreshToken = "valid_refresh_token"
        };
        await _userRepository.CreateAsync(user);
        
        // Setup mock: User is NOT currently playing music.
        _mockSpotifyService
            .Setup(s => s.IsUserPlayingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        // Act: Trigger auto-queue.
        await _autoQueueService.TriggerAutoQueueAsync(chatId, trackId, CancellationToken.None);
        
        // Assert: Verify IsUserPlayingAsync was called.
        _mockSpotifyService.Verify(
            s => s.IsUserPlayingAsync("valid_access_token", It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify AddTrackToQueueAsync was NOT called.
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 41: Auto-Queue Conditional Execution")]
    public async Task AutoQueue_WhenDisabled_DoesNotAddToQueue(
        long chatId,
        long userId,
        string trackId)
    {
        // Arrange: Create user with auto-queue DISABLED.
        var userConfig = new UserGroupConfigEntity(chatId, userId)
        {
            AutoQueueEnabled = false
        };
        await _userGroupConfigRepository.UpsertAsync(userConfig);
        
        // Create user with valid credentials.
        var user = new UserEntity(userId)
        {
            EncryptedAccessToken = "valid_access_token",
            EncryptedRefreshToken = "valid_refresh_token"
        };
        await _userRepository.CreateAsync(user);
        
        // Act: Trigger auto-queue.
        await _autoQueueService.TriggerAutoQueueAsync(chatId, trackId, CancellationToken.None);
        
        // Assert: Verify neither IsUserPlayingAsync nor AddTrackToQueueAsync were called.
        _mockSpotifyService.Verify(
            s => s.IsUserPlayingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 41: Auto-Queue Conditional Execution")]
    public async Task AutoQueue_WhenUserHasNoCredentials_DoesNotAddToQueue(
        long chatId,
        long userId,
        string trackId)
    {
        // Arrange: Create user with auto-queue enabled.
        var userConfig = new UserGroupConfigEntity(chatId, userId)
        {
            AutoQueueEnabled = true
        };
        await _userGroupConfigRepository.UpsertAsync(userConfig);
        
        // Create user WITHOUT credentials.
        var user = new UserEntity(userId);
        await _userRepository.CreateAsync(user);
        
        // Act: Trigger auto-queue.
        await _autoQueueService.TriggerAutoQueueAsync(chatId, trackId, CancellationToken.None);
        
        // Assert: Verify neither IsUserPlayingAsync nor AddTrackToQueueAsync were called.
        _mockSpotifyService.Verify(
            s => s.IsUserPlayingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(12345, 67890, 11111, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, 44444, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 41: Auto-Queue Conditional Execution")]
    public async Task AutoQueue_WithMultipleUsers_OnlyAddsForPlayingUsers(
        long chatId,
        long userId1,
        long userId2,
        string trackId)
    {
        // Arrange: Create two users with auto-queue enabled.
        var userConfig1 = new UserGroupConfigEntity(chatId, userId1)
        {
            AutoQueueEnabled = true
        };
        await _userGroupConfigRepository.UpsertAsync(userConfig1);
        
        var userConfig2 = new UserGroupConfigEntity(chatId, userId2)
        {
            AutoQueueEnabled = true
        };
        await _userGroupConfigRepository.UpsertAsync(userConfig2);
        
        // Create users with valid credentials.
        var user1 = new UserEntity(userId1)
        {
            EncryptedAccessToken = "user1_token",
            EncryptedRefreshToken = "user1_refresh"
        };
        await _userRepository.CreateAsync(user1);
        
        var user2 = new UserEntity(userId2)
        {
            EncryptedAccessToken = "user2_token",
            EncryptedRefreshToken = "user2_refresh"
        };
        await _userRepository.CreateAsync(user2);
        
        // Setup mock: User1 is playing, User2 is not.
        _mockSpotifyService
            .Setup(s => s.IsUserPlayingAsync("user1_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockSpotifyService
            .Setup(s => s.IsUserPlayingAsync("user2_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _mockSpotifyService
            .Setup(s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        // Act: Trigger auto-queue.
        await _autoQueueService.TriggerAutoQueueAsync(chatId, trackId, CancellationToken.None);
        
        // Assert: Verify IsUserPlayingAsync was called for both users.
        _mockSpotifyService.Verify(
            s => s.IsUserPlayingAsync("user1_token", It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockSpotifyService.Verify(
            s => s.IsUserPlayingAsync("user2_token", It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify AddTrackToQueueAsync was only called for user1 (who is playing).
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(trackId, "user1_token", It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(trackId, "user2_token", It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
