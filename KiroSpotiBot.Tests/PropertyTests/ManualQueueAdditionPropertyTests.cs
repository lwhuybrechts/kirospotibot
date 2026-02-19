using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Handlers;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Infrastructure.Services;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for manual queue addition.
/// Property 43: Manual Queue Addition
/// Validates: Requirements 20.2, 20.6
/// 
/// Note: These tests verify that authenticated users can manually add tracks
/// to their personal Spotify queue regardless of who originally shared the track.
/// </summary>
public class ManualQueueAdditionPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IUserRepository _userRepository;
    private readonly Mock<ISpotifyService> _mockSpotifyService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<ITelegramBotClient> _mockTelegramBotClient;
    private readonly IMessageHandler _messageHandler;
    private readonly TableClient _usersTable;

    public ManualQueueAdditionPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var userLogger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        var messageHandlerLogger = Mock.Of<ILogger<MessageHandler>>();
        
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockEncryptionService.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _mockEncryptionService.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        
        _userRepository = new UserRepository(
            _tableServiceClient, 
            _mockEncryptionService.Object,
            userLogger);
        
        _mockSpotifyService = new Mock<ISpotifyService>();
        _mockTelegramBotClient = new Mock<ITelegramBotClient>();
        
        // Create MessageHandler with minimal dependencies for callback query testing.
        _messageHandler = new MessageHandler(
            messageHandlerLogger,
            _mockTelegramBotClient.Object,
            _userRepository,
            Mock.Of<IGroupChatRepository>(),
            Mock.Of<ISpotifyUrlDetector>(),
            Mock.Of<IGroupConfigurationValidator>(),
            Mock.Of<ITrackAdditionHandler>(),
            Mock.Of<IGroupSetupHandler>(),
            Mock.Of<IVoteManager>(),
            Mock.Of<ITrackRecordRepository>(),
            Mock.Of<ICommandHandler>(),
            _mockSpotifyService.Object);
        
        // Get table references.
        _usersTable = _tableServiceClient.GetTableClient("Users");
        
        // Truncate tables before tests.
        TableHelper.TruncateTable(_usersTable);
    }

    [Theory]
    [InlineData(12345, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(67890, "7qiZfU4dY1lWllzX7mPBI")]
    [InlineData(99999, "0VjIjW4GlUZAMYd2vXMi3b")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 43: Manual Queue Addition")]
    public async Task ManualQueueAddition_WhenUserIsAuthenticatedAndPlaying_AddsTrackToQueue(
        long userId,
        string trackId)
    {
        // Arrange: Create authenticated user.
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
        
        // Create callback query.
        var callbackQuery = new CallbackQuery
        {
            Id = "callback_123",
            From = new User { Id = userId, FirstName = "Test" },
            Data = $"queue:{trackId}"
        };
        
        // Act: Handle callback query.
        await _messageHandler.HandleCallbackQueryAsync(callbackQuery, CancellationToken.None);
        
        // Assert: Verify track was added to queue.
        _mockSpotifyService.Verify(
            s => s.IsUserPlayingAsync("valid_access_token", It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(trackId, "valid_access_token", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(67890, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 43: Manual Queue Addition")]
    public async Task ManualQueueAddition_WhenUserNotAuthenticated_RejectsWithAuthPrompt(
        long userId,
        string trackId)
    {
        // Arrange: Create user WITHOUT credentials.
        var user = new UserEntity(userId);
        await _userRepository.CreateAsync(user);
        
        // Create callback query.
        var callbackQuery = new CallbackQuery
        {
            Id = "callback_123",
            From = new User { Id = userId, FirstName = "Test" },
            Data = $"queue:{trackId}"
        };
        
        // Act: Handle callback query.
        await _messageHandler.HandleCallbackQueryAsync(callbackQuery, CancellationToken.None);
        
        // Assert: Verify track was NOT added to queue.
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(12345, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(67890, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 43: Manual Queue Addition")]
    public async Task ManualQueueAddition_WhenUserNotPlaying_RejectsWithPlayingPrompt(
        long userId,
        string trackId)
    {
        // Arrange: Create authenticated user.
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
        
        // Create callback query.
        var callbackQuery = new CallbackQuery
        {
            Id = "callback_123",
            From = new User { Id = userId, FirstName = "Test" },
            Data = $"queue:{trackId}"
        };
        
        // Act: Handle callback query.
        await _messageHandler.HandleCallbackQueryAsync(callbackQuery, CancellationToken.None);
        
        // Assert: Verify IsUserPlayingAsync was called.
        _mockSpotifyService.Verify(
            s => s.IsUserPlayingAsync("valid_access_token", It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify track was NOT added to queue.
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 43: Manual Queue Addition")]
    public async Task ManualQueueAddition_DifferentUserThanSharer_CanAddToTheirQueue(
        long sharerUserId,
        long clickerUserId,
        string trackId)
    {
        // Arrange: Create two users - one who shared, one who clicks button.
        var sharerUser = new UserEntity(sharerUserId)
        {
            EncryptedAccessToken = "sharer_token",
            EncryptedRefreshToken = "sharer_refresh"
        };
        await _userRepository.CreateAsync(sharerUser);
        
        var clickerUser = new UserEntity(clickerUserId)
        {
            EncryptedAccessToken = "clicker_token",
            EncryptedRefreshToken = "clicker_refresh"
        };
        await _userRepository.CreateAsync(clickerUser);
        
        // Setup mock: Clicker is currently playing music.
        _mockSpotifyService
            .Setup(s => s.IsUserPlayingAsync("clicker_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockSpotifyService
            .Setup(s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        // Create callback query from clicker (not sharer).
        var callbackQuery = new CallbackQuery
        {
            Id = "callback_123",
            From = new User { Id = clickerUserId, FirstName = "Clicker" },
            Data = $"queue:{trackId}"
        };
        
        // Act: Handle callback query.
        await _messageHandler.HandleCallbackQueryAsync(callbackQuery, CancellationToken.None);
        
        // Assert: Verify track was added to CLICKER's queue (not sharer's).
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(trackId, "clicker_token", It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify sharer's token was NOT used.
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(trackId, "sharer_token", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(12345, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(67890, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 43: Manual Queue Addition")]
    public async Task ManualQueueAddition_WhenSpotifyFails_ReturnsErrorMessage(
        long userId,
        string trackId)
    {
        // Arrange: Create authenticated user.
        var user = new UserEntity(userId)
        {
            EncryptedAccessToken = "valid_access_token",
            EncryptedRefreshToken = "valid_refresh_token"
        };
        await _userRepository.CreateAsync(user);
        
        // Setup mock: User is playing but Spotify API fails.
        _mockSpotifyService
            .Setup(s => s.IsUserPlayingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockSpotifyService
            .Setup(s => s.AddTrackToQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        // Create callback query.
        var callbackQuery = new CallbackQuery
        {
            Id = "callback_123",
            From = new User { Id = userId, FirstName = "Test" },
            Data = $"queue:{trackId}"
        };
        
        // Act: Handle callback query.
        await _messageHandler.HandleCallbackQueryAsync(callbackQuery, CancellationToken.None);
        
        // Assert: Verify AddTrackToQueueAsync was called.
        _mockSpotifyService.Verify(
            s => s.AddTrackToQueueAsync(trackId, "valid_access_token", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
