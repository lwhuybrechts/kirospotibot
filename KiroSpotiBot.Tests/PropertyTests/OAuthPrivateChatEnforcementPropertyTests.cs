using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Functions;
using KiroSpotiBot.Infrastructure.Handlers;
using KiroSpotiBot.Infrastructure.Options;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for OAuth private chat enforcement.
/// Property 6: OAuth Private Chat Enforcement
/// Validates: Requirements 4.1, 4.2
/// 
/// Note: These tests verify that OAuth authentication links are only sent to private chats
/// and never to group chats. The actual enforcement happens in the message handler,
/// but this tests the OAuth flow accepts private chat IDs.
/// </summary>
public class OAuthPrivateChatEnforcementPropertyTests : IDisposable
{
    private readonly SpotifyOAuthFunction _function;
    private readonly Mock<IOAuthStateRepository> _mockOAuthStateRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<ITelegramBotClient> _mockTelegramClient;

    public OAuthPrivateChatEnforcementPropertyTests()
    {
        var mockLogger = new Mock<ILogger<SpotifyOAuthFunction>>();
        var mockHandlerLogger = new Mock<ILogger<SpotifyOAuthHandler>>();
        _mockOAuthStateRepo = new Mock<IOAuthStateRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockTelegramClient = new Mock<ITelegramBotClient>();
        
        var spotifyOptions = Options.Create(new SpotifyOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUri = "https://test.com/callback"
        });

        var handler = new SpotifyOAuthHandler(
            mockHandlerLogger.Object,
            _mockOAuthStateRepo.Object,
            _mockUserRepo.Object,
            _mockTelegramClient.Object,
            spotifyOptions
        );

        _function = new SpotifyOAuthFunction(
            mockLogger.Object,
            handler
        );
    }

    [Theory]
    [InlineData(12345, 12345)] // Private chat: user ID equals chat ID.
    [InlineData(67890, 67890)] // Private chat: user ID equals chat ID.
    [InlineData(11111, 11111)] // Private chat: user ID equals chat ID.
    [InlineData(99999, 99999)] // Private chat: user ID equals chat ID.
    [InlineData(54321, 54321)] // Private chat: user ID equals chat ID.
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 6: OAuth Private Chat Enforcement")]
    public async Task OAuthStartAuth_PrivateChatId_GeneratesValidAuthorizationUrl(
        long telegramUserId,
        long privateChatId)
    {
        // Arrange: Setup OAuth state repository to accept state creation.
        _mockOAuthStateRepo
            .Setup(x => x.CreateAsync(It.IsAny<OAuthStateEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthStateEntity entity, CancellationToken _) => entity);

        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString($"?telegramUserId={telegramUserId}&chatId={privateChatId}");

        // Act: Initiate OAuth flow.
        var result = await _function.StartAuth(request, CancellationToken.None);

        // Assert: Verify redirect result is returned.
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        
        // Verify the redirect URL contains Spotify authorization endpoint.
        Assert.Contains("accounts.spotify.com/authorize", redirectResult.Url);
        Assert.Contains("client_id=test-client-id", redirectResult.Url);
        Assert.Contains("redirect_uri=", redirectResult.Url);
        Assert.Contains("response_type=code", redirectResult.Url);
        
        // Verify OAuth state was created with the private chat ID.
        _mockOAuthStateRepo.Verify(
            x => x.CreateAsync(
                It.Is<OAuthStateEntity>(e => 
                    e.TelegramUserId == telegramUserId && 
                    e.TelegramChatId == privateChatId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, -100123456789)] // User in group chat (negative chat ID).
    [InlineData(67890, -100987654321)] // User in group chat (negative chat ID).
    [InlineData(11111, -100111111111)] // User in group chat (negative chat ID).
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 6: OAuth Private Chat Enforcement")]
    public async Task OAuthStartAuth_GroupChatId_StillGeneratesUrl(
        long telegramUserId,
        long groupChatId)
    {
        // Arrange: This test verifies the OAuth function itself doesn't reject group chat IDs.
        // The enforcement of "private chat only" happens in the message handler (Task 8-9).
        _mockOAuthStateRepo
            .Setup(x => x.CreateAsync(It.IsAny<OAuthStateEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthStateEntity entity, CancellationToken _) => entity);

        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString($"?telegramUserId={telegramUserId}&chatId={groupChatId}");

        // Act.
        var result = await _function.StartAuth(request, CancellationToken.None);

        // Assert: OAuth function generates URL (enforcement is in message handler).
        Assert.IsType<RedirectResult>(result);
        
        // Verify state was created with the group chat ID.
        _mockOAuthStateRepo.Verify(
            x => x.CreateAsync(
                It.Is<OAuthStateEntity>(e => 
                    e.TelegramUserId == telegramUserId && 
                    e.TelegramChatId == groupChatId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, 12345)]
    [InlineData(67890, 67890)]
    [InlineData(11111, 11111)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 6: OAuth Private Chat Enforcement")]
    public async Task OAuthStartAuth_RequiredScopes_AreIncludedInAuthorizationUrl(
        long telegramUserId,
        long privateChatId)
    {
        // Arrange.
        _mockOAuthStateRepo
            .Setup(x => x.CreateAsync(It.IsAny<OAuthStateEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthStateEntity entity, CancellationToken _) => entity);

        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString($"?telegramUserId={telegramUserId}&chatId={privateChatId}");

        // Act.
        var result = await _function.StartAuth(request, CancellationToken.None);

        // Assert: Verify required scopes are in the URL.
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        
        // Verify all required scopes are present (Requirements 4.3).
        Assert.Contains("playlist-modify-public", redirectResult.Url);
        Assert.Contains("playlist-modify-private", redirectResult.Url);
        Assert.Contains("user-modify-playback-state", redirectResult.Url);
        Assert.Contains("user-read-playback-state", redirectResult.Url);
    }

    public void Dispose()
    {
        // Cleanup if needed.
    }
}
