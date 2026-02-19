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
/// Property-based tests for administrator-only commands.
/// Property 14: Administrator-Only Commands
/// Validates: Requirements 8.1, 8.3
/// 
/// Note: These tests verify that configuration commands (playlist setup, threshold change)
/// succeed when invoked by the administrator and fail with authorization error when invoked
/// by non-administrators.
/// </summary>
public class AdministratorOnlyCommandsPropertyTests
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

    public AdministratorOnlyCommandsPropertyTests()
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
        _mockSpotifyService
            .Setup(x => x.ValidatePlaylistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSpotifyService
            .Setup(x => x.GetPlaylistNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Playlist");
        
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
    [InlineData(-1001234567890, 12345, "playlist_id_1")]
    [InlineData(-1009876543210, 67890, "playlist_id_2")]
    [InlineData(-1001111111111, 11111, "playlist_id_3")]
    [InlineData(-1002222222222, 22222, "playlist_id_4")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 14: Administrator-Only Commands")]
    public async Task ConfigureCommand_InvokedByAdministrator_SuccessfullyConfiguresPlaylist(
        long chatId,
        long administratorUserId,
        string playlistId)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        
        // Create and authenticate administrator.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        await _userRepository.UpdateSpotifyCredentialsAsync(
            administratorUserId,
            "access_token",
            "refresh_token",
            3600,
            "playlist-modify-public",
            CancellationToken.None);
        
        // Create message from administrator.
        var adminUser = MessageHelper.CreateUser(administratorUserId, "admin");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        var message = MessageHelper.CreateMessage(1, adminUser, chat, DateTime.UtcNow, $"/configure {playlistId}");
        
        // Act: Administrator invokes /configure command.
        await _commandHandler.HandleConfigureCommandAsync(message, CancellationToken.None);
        
        // Assert: Verify playlist was configured.
        var updatedGroupChat = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.NotNull(updatedGroupChat);
        Assert.Equal(playlistId, updatedGroupChat.PlaylistId);
        Assert.Equal("Test Playlist", updatedGroupChat.PlaylistName);
        
        // Verify success message was sent.
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("✅") && 
                    r.Text.Contains("configured successfully")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1001234567890, 12345, 54321, "playlist_id_1")]
    [InlineData(-1009876543210, 67890, 98765, "playlist_id_2")]
    [InlineData(-1001111111111, 11111, 99999, "playlist_id_3")]
    [InlineData(-1002222222222, 22222, 33333, "playlist_id_4")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 14: Administrator-Only Commands")]
    public async Task ConfigureCommand_InvokedByNonAdministrator_FailsWithAuthorizationError(
        long chatId,
        long administratorUserId,
        long nonAdminUserId,
        string playlistId)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        
        // Create and authenticate administrator.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        await _userRepository.UpdateSpotifyCredentialsAsync(
            administratorUserId,
            "access_token",
            "refresh_token",
            3600,
            "playlist-modify-public",
            CancellationToken.None);
        
        // Create non-administrator user.
        await _userRepository.CreateUserAsync(nonAdminUserId, CancellationToken.None);
        
        // Create message from non-administrator.
        var nonAdminUser = MessageHelper.CreateUser(nonAdminUserId, "member");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        var message = MessageHelper.CreateMessage(1, nonAdminUser, chat, DateTime.UtcNow, $"/configure {playlistId}");
        
        // Act: Non-administrator invokes /configure command.
        await _commandHandler.HandleConfigureCommandAsync(message, CancellationToken.None);
        
        // Assert: Verify playlist was NOT configured.
        var updatedGroupChat = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.NotNull(updatedGroupChat);
        Assert.Null(updatedGroupChat.PlaylistId); // Should remain null.
        
        // Verify authorization error message was sent.
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("❌") && 
                    r.Text.Contains("administrator")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1001234567890, 12345, 5)]
    [InlineData(-1009876543210, 67890, 3)]
    [InlineData(-1001111111111, 11111, 10)]
    [InlineData(-1002222222222, 22222, 7)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 14: Administrator-Only Commands")]
    public async Task ThresholdCommand_InvokedByAdministrator_SuccessfullyUpdatesThreshold(
        long chatId,
        long administratorUserId,
        int newThreshold)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        
        // Create administrator user.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        
        // Create message from administrator.
        var adminUser = MessageHelper.CreateUser(administratorUserId, "admin");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        var message = MessageHelper.CreateMessage(1, adminUser, chat, DateTime.UtcNow, $"/threshold {newThreshold}");
        
        // Act: Administrator invokes /threshold command.
        await _commandHandler.HandleThresholdCommandAsync(message, CancellationToken.None);
        
        // Assert: Verify threshold was updated.
        var updatedGroupChat = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.NotNull(updatedGroupChat);
        Assert.Equal(newThreshold, updatedGroupChat.DownvoteThreshold);
        
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
    [InlineData(-1001234567890, 12345, 54321, 5)]
    [InlineData(-1009876543210, 67890, 98765, 8)]
    [InlineData(-1001111111111, 11111, 99999, 10)]
    [InlineData(-1002222222222, 22222, 33333, 7)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 14: Administrator-Only Commands")]
    public async Task ThresholdCommand_InvokedByNonAdministrator_FailsWithAuthorizationError(
        long chatId,
        long administratorUserId,
        long nonAdminUserId,
        int newThreshold)
    {
        // Arrange: Create group chat with administrator and default threshold.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        var originalThreshold = groupChat.DownvoteThreshold;
        
        // Create administrator user.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        
        // Create non-administrator user.
        await _userRepository.CreateUserAsync(nonAdminUserId, CancellationToken.None);
        
        // Create message from non-administrator.
        var nonAdminUser = MessageHelper.CreateUser(nonAdminUserId, "member");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        var message = MessageHelper.CreateMessage(1, nonAdminUser, chat, DateTime.UtcNow, $"/threshold {newThreshold}");
        
        // Act: Non-administrator invokes /threshold command.
        await _commandHandler.HandleThresholdCommandAsync(message, CancellationToken.None);
        
        // Assert: Verify threshold was NOT updated.
        var updatedGroupChat = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.NotNull(updatedGroupChat);
        Assert.Equal(originalThreshold, updatedGroupChat.DownvoteThreshold); // Should remain unchanged.
        Assert.NotEqual(newThreshold, updatedGroupChat.DownvoteThreshold);
        
        // Verify authorization error message was sent.
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("❌") && 
                    r.Text.Contains("administrator")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1001234567890, 12345, 54321, "playlist_1", 5)]
    [InlineData(-1009876543210, 67890, 98765, "playlist_2", 8)]
    [InlineData(-1001111111111, 11111, 99999, "playlist_3", 3)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 14: Administrator-Only Commands")]
    public async Task MultipleConfigurationCommands_AdministratorAndNonAdmin_OnlyAdministratorSucceeds(
        long chatId,
        long administratorUserId,
        long nonAdminUserId,
        string playlistId,
        int threshold)
    {
        // Arrange: Create group chat with administrator.
        var groupChat = await _groupChatRepository.CreateGroupChatAsync(chatId, administratorUserId, CancellationToken.None);
        var originalThreshold = groupChat.DownvoteThreshold;
        
        // Create and authenticate administrator.
        await _userRepository.CreateUserAsync(administratorUserId, CancellationToken.None);
        await _userRepository.UpdateSpotifyCredentialsAsync(
            administratorUserId,
            "access_token",
            "refresh_token",
            3600,
            "playlist-modify-public",
            CancellationToken.None);
        
        // Create non-administrator user.
        await _userRepository.CreateUserAsync(nonAdminUserId, CancellationToken.None);
        
        // Create users and chat.
        var nonAdminUser = MessageHelper.CreateUser(nonAdminUserId, "member");
        var adminUser = MessageHelper.CreateUser(administratorUserId, "admin");
        var chat = MessageHelper.CreateChat(chatId, ChatType.Group);
        
        // Act 1: Non-administrator tries to configure playlist.
        var nonAdminConfigureMessage = MessageHelper.CreateMessage(1, nonAdminUser, chat, DateTime.UtcNow, $"/configure {playlistId}");
        await _commandHandler.HandleConfigureCommandAsync(nonAdminConfigureMessage, CancellationToken.None);
        
        // Assert 1: Playlist should NOT be configured.
        var afterNonAdminConfigure = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.Null(afterNonAdminConfigure!.PlaylistId);
        
        // Act 2: Administrator configures playlist.
        var adminConfigureMessage = MessageHelper.CreateMessage(2, adminUser, chat, DateTime.UtcNow, $"/configure {playlistId}");
        await _commandHandler.HandleConfigureCommandAsync(adminConfigureMessage, CancellationToken.None);
        
        // Assert 2: Playlist should be configured.
        var afterAdminConfigure = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.Equal(playlistId, afterAdminConfigure!.PlaylistId);
        
        // Act 3: Non-administrator tries to change threshold.
        var nonAdminThresholdMessage = MessageHelper.CreateMessage(3, nonAdminUser, chat, DateTime.UtcNow, $"/threshold {threshold}");
        await _commandHandler.HandleThresholdCommandAsync(nonAdminThresholdMessage, CancellationToken.None);
        
        // Assert 3: Threshold should NOT be changed.
        var afterNonAdminThreshold = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.Equal(originalThreshold, afterNonAdminThreshold!.DownvoteThreshold);
        
        // Act 4: Administrator changes threshold.
        var adminThresholdMessage = MessageHelper.CreateMessage(4, adminUser, chat, DateTime.UtcNow, $"/threshold {threshold}");
        await _commandHandler.HandleThresholdCommandAsync(adminThresholdMessage, CancellationToken.None);
        
        // Assert 4: Threshold should be changed.
        var afterAdminThreshold = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, CancellationToken.None);
        Assert.Equal(threshold, afterAdminThreshold!.DownvoteThreshold);
        
        // Verify authorization error messages were sent for non-admin attempts (2 times).
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("❌") && 
                    r.Text.Contains("administrator")),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        
        // Verify success messages were sent for admin commands (2 times).
        _mockTelegramBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId.Identifier == chatId &&
                    r.Text.Contains("✅")),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}


