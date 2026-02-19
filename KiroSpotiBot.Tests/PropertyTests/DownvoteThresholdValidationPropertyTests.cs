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
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for downvote threshold validation.
/// Property 28: Downvote Threshold Validation
/// Validates: Requirements 18.3
/// 
/// Note: These tests verify that non-positive integers are rejected with a validation error
/// when attempting to configure the downvote threshold.
/// </summary>
public class DownvoteThresholdValidationPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IGroupChatRepository _groupChatRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserGroupConfigRepository _userGroupConfigRepository;
    private readonly Mock<ITelegramBotClient> _mockTelegramBotClient;
    private readonly Mock<ISpotifyService> _mockSpotifyService;
    private readonly Mock<ISpotifyOAuthHandler> _mockOAuthHandler;
    private readonly ICommandHandler _commandHandler;
    private readonly TableClient _groupChatsTableClient;
    private readonly TableClient _usersTableClient;
    private readonly TableClient _userGroupConfigsTableClient;

    public DownvoteThresholdValidationPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var groupChatLogger = Mock.Of<ILogger<BaseRepository<GroupChatEntity>>>();
        var userLogger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        var userGroupConfigLogger = Mock.Of<ILogger<BaseRepository<UserGroupConfigEntity>>>();
        var commandHandlerLogger = Mock.Of<ILogger<CommandHandler>>();
        
        var encryptionService = new Mock<IEncryptionService>();
        
        // Setup encryption service to return predictable encrypted values.
        encryptionService
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string input) => $"encrypted_{input}");
        
        encryptionService
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Returns((string input) => input.Replace("encrypted_", ""));
        
        _groupChatRepository = new GroupChatRepository(_tableServiceClient, groupChatLogger);
        _userRepository = new UserRepository(_tableServiceClient, encryptionService.Object, userLogger);
        _userGroupConfigRepository = new UserGroupConfigRepository(_tableServiceClient, userGroupConfigLogger);
        
        // Setup mock Telegram bot client.
        _mockTelegramBotClient = new Mock<ITelegramBotClient>();
        _mockTelegramBotClient
            .Setup(x => x.SendRequest(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message)null!);
        
        // Setup mock Spotify service.
        _mockSpotifyService = new Mock<ISpotifyService>();
        
        // Setup mock OAuth handler.
        _mockOAuthHandler = new Mock<ISpotifyOAuthHandler>();
        
        _commandHandler = new CommandHandler(
            commandHandlerLogger,
            _mockTelegramBotClient.Object,
            _mockOAuthHandler.Object,
            _userRepository,
            _groupChatRepository,
            _userGroupConfigRepository,
            _mockSpotifyService.Object);
        
        // Get references to tables.
        _groupChatsTableClient = _tableServiceClient.GetTableClient("GroupChats");
        _usersTableClient = _tableServiceClient.GetTableClient("Users");
        _userGroupConfigsTableClient = _tableServiceClient.GetTableClient("UserGroupConfigs");
        
        // Truncate tables before tests to ensure clean state.
        TableHelper.TruncateTable(_groupChatsTableClient);
        TableHelper.TruncateTable(_usersTableClient);
        TableHelper.TruncateTable(_userGroupConfigsTableClient);
    }

    [Theory]
    [InlineData(-1001234567890, 12345, 0)]
    [InlineData(-1009876543210, 67890, -1)]
    [InlineData(-1001111111111, 11111, -5)]
    [InlineData(-1002222222222, 22222, -100)]
    [InlineData(-1003333333333, 33333, -999)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 28: Downvote Threshold Validation")]
    public async Task ThresholdCommand_WithNonPositiveInteger_RejectsWithValidationError(
        long chatId,
        long administratorUserId,
        int invalidThreshold)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        var originalThreshold = groupChat.DownvoteThreshold;
        
        // Create administrator user.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        
        // Create message from administrator with invalid threshold.
        var adminUser = MessageHelper.CreateUser(administratorUserId, "admin");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        var message = MessageHelper.CreateMessage(1, adminUser, chat, DateTime.UtcNow, $"/threshold {invalidThreshold}");
        
        // Act: Administrator attempts to set invalid threshold.
        await _commandHandler.HandleThresholdCommandAsync(message, CancellationToken.None);
        
        // Assert: Verify threshold was NOT updated.
        var updatedGroupChat = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.NotNull(updatedGroupChat);
        Assert.Equal(originalThreshold, updatedGroupChat.DownvoteThreshold); // Should remain unchanged.
        Assert.NotEqual(invalidThreshold, updatedGroupChat.DownvoteThreshold);
        
        // Verify validation error message was sent.
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("❌") && 
                    r.Text.Contains("positive integer")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1001234567890, 12345, "abc")]
    [InlineData(-1009876543210, 67890, "invalid")]
    [InlineData(-1001111111111, 11111, "3.5")]
    [InlineData(-1003333333333, 33333, "!@#")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 28: Downvote Threshold Validation")]
    public async Task ThresholdCommand_WithNonIntegerValue_RejectsWithValidationError(
        long chatId,
        long administratorUserId,
        string invalidThreshold)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        var originalThreshold = groupChat.DownvoteThreshold;
        
        // Create administrator user.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        
        // Create message from administrator with non-integer threshold.
        var adminUser = MessageHelper.CreateUser(administratorUserId, "admin");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        var message = MessageHelper.CreateMessage(1, adminUser, chat, DateTime.UtcNow, $"/threshold {invalidThreshold}");
        
        // Act: Administrator attempts to set non-integer threshold.
        await _commandHandler.HandleThresholdCommandAsync(message, CancellationToken.None);
        
        // Assert: Verify threshold was NOT updated.
        var updatedGroupChat = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.NotNull(updatedGroupChat);
        Assert.Equal(originalThreshold, updatedGroupChat.DownvoteThreshold); // Should remain unchanged.
        
        // Verify validation error message was sent.
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("❌") && 
                    r.Text.Contains("positive integer")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1001234567890, 12345, 1)]
    [InlineData(-1009876543210, 67890, 3)]
    [InlineData(-1001111111111, 11111, 5)]
    [InlineData(-1002222222222, 22222, 10)]
    [InlineData(-1003333333333, 33333, 100)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 28: Downvote Threshold Validation")]
    public async Task ThresholdCommand_WithPositiveInteger_AcceptsAndUpdatesThreshold(
        long chatId,
        long administratorUserId,
        int validThreshold)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        
        // Create administrator user.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        
        // Create message from administrator with valid threshold.
        var adminUser = MessageHelper.CreateUser(administratorUserId, "admin");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        var message = MessageHelper.CreateMessage(1, adminUser, chat, DateTime.UtcNow, $"/threshold {validThreshold}");
        
        // Act: Administrator sets valid threshold.
        await _commandHandler.HandleThresholdCommandAsync(message, CancellationToken.None);
        
        // Assert: Verify threshold was updated.
        var updatedGroupChat = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.NotNull(updatedGroupChat);
        Assert.Equal(validThreshold, updatedGroupChat.DownvoteThreshold);
        
        // Verify success message was sent.
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("✅") && 
                    r.Text.Contains("threshold updated")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1001234567890, 12345, 0, -5, 10)]
    [InlineData(-1009876543210, 67890, -1, -10, 3)]
    [InlineData(-1001111111111, 11111, -100, 0, 7)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 28: Downvote Threshold Validation")]
    public async Task ThresholdCommand_MultipleInvalidAttempts_AllRejectedWithValidationError(
        long chatId,
        long administratorUserId,
        int invalidThreshold1,
        int invalidThreshold2,
        int validThreshold)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        var originalThreshold = groupChat.DownvoteThreshold;
        
        // Create administrator user.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        
        // Create user and chat.
        var adminUser = MessageHelper.CreateUser(administratorUserId, "admin");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        
        // Act 1: First invalid attempt.
        var message1 = MessageHelper.CreateMessage(1, adminUser, chat, DateTime.UtcNow, $"/threshold {invalidThreshold1}");
        await _commandHandler.HandleThresholdCommandAsync(message1, CancellationToken.None);
        
        // Assert 1: Threshold should remain unchanged.
        var afterFirstAttempt = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.Equal(originalThreshold, afterFirstAttempt!.DownvoteThreshold);
        
        // Act 2: Second invalid attempt.
        var message2 = MessageHelper.CreateMessage(2, adminUser, chat, DateTime.UtcNow, $"/threshold {invalidThreshold2}");
        await _commandHandler.HandleThresholdCommandAsync(message2, CancellationToken.None);
        
        // Assert 2: Threshold should still remain unchanged.
        var afterSecondAttempt = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.Equal(originalThreshold, afterSecondAttempt!.DownvoteThreshold);
        
        // Act 3: Valid attempt.
        var message3 = MessageHelper.CreateMessage(3, adminUser, chat, DateTime.UtcNow, $"/threshold {validThreshold}");
        await _commandHandler.HandleThresholdCommandAsync(message3, CancellationToken.None);
        
        // Assert 3: Threshold should now be updated.
        var afterValidAttempt = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.Equal(validThreshold, afterValidAttempt!.DownvoteThreshold);
        
        // Verify validation error messages were sent for invalid attempts (2 times).
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("❌") && 
                    r.Text.Contains("positive integer")),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        
        // Verify success message was sent for valid attempt (1 time).
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("✅") && 
                    r.Text.Contains("threshold updated")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
