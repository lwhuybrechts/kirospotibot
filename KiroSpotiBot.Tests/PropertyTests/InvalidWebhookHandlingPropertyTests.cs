using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Functions;
using KiroSpotiBot.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot.Types;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for invalid webhook handling validation.
/// Property 2: Invalid Webhook Handling
/// Validates: Requirements 1.3, 10.3
/// 
/// For any invalid or malformed webhook request, the bot should log the error
/// and return an appropriate HTTP status code without crashing.
/// 
/// Note: These tests use xUnit's Theory attribute with InlineData to simulate
/// property-based testing behavior by testing multiple input combinations.
/// </summary>
public class InvalidWebhookHandlingPropertyTests
{
    private readonly Mock<ILogger<TelegramWebhookFunction>> _loggerMock;
    private readonly Mock<ITelegramUpdateHandler> _handlerMock;
    private readonly IOptions<TelegramOptions> _telegramOptions;

    public InvalidWebhookHandlingPropertyTests()
    {
        _loggerMock = new Mock<ILogger<TelegramWebhookFunction>>();
        _handlerMock = new Mock<ITelegramUpdateHandler>();
        _telegramOptions = Options.Create(new TelegramOptions
        {
            BotToken = "test-token",
            WebhookSecretToken = string.Empty // Disable signature validation for these tests.
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("{]")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 2: Invalid Webhook Handling")]
    public async Task InvalidWebhookHandling_MalformedJson_ReturnsBadRequest(string malformedJson)
    {
        // Arrange: Create request with malformed JSON.
        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(malformedJson);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act: Process the webhook request.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify BadRequest is returned and error is logged.
        Assert.IsType<BadRequestObjectResult>(result);
        
        // Verify error was logged.
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify handler was never called.
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 2: Invalid Webhook Handling")]
    public async Task InvalidWebhookHandling_NullJsonLiteral_ReturnsBadRequest()
    {
        // Arrange: Create request with JSON literal "null" that deserializes to null update.
        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes("null");
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act: Process the webhook request.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify BadRequest is returned and warning is logged.
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.NotNull(badRequestResult.Value);
        
        // Verify warning was logged.
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("null update")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify handler was never called.
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 2: Invalid Webhook Handling")]
    public async Task InvalidWebhookHandling_EmptyBody_ReturnsBadRequest()
    {
        // Arrange: Create request with empty body.
        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        request.Body = new MemoryStream();
        request.ContentType = "application/json";

        // Act: Process the webhook request.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify BadRequest is returned.
        Assert.IsType<BadRequestObjectResult>(result);
        
        // Verify handler was never called.
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("wrong-token")]
    [InlineData("")]
    [InlineData("invalid")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 2: Invalid Webhook Handling")]
    public async Task InvalidWebhookHandling_InvalidSignature_ReturnsUnauthorized(string invalidToken)
    {
        // Arrange: Create request with invalid signature.
        var telegramOptionsWithSecret = Options.Create(new TelegramOptions
        {
            BotToken = "test-token",
            WebhookSecretToken = "correct-secret-token"
        });
        
        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, telegramOptionsWithSecret);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        // Add invalid secret token header.
        request.Headers["X-Telegram-Bot-Api-Secret-Token"] = invalidToken;
        
        var json = """
        {
            "update_id": 123456,
            "message": {
                "message_id": 1,
                "date": 1705315800,
                "chat": {
                    "id": 12345,
                    "type": "group"
                },
                "from": {
                    "id": 67890,
                    "is_bot": false,
                    "first_name": "Test"
                },
                "text": "Test message"
            }
        }
        """;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act: Process the webhook request.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify Unauthorized is returned and warning is logged.
        Assert.IsType<UnauthorizedResult>(result);
        
        // Verify warning was logged.
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid webhook signature")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify handler was never called.
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 2: Invalid Webhook Handling")]
    public async Task InvalidWebhookHandling_MissingSignatureHeader_ReturnsUnauthorized()
    {
        // Arrange: Create request without signature header when signature is required.
        var telegramOptionsWithSecret = Options.Create(new TelegramOptions
        {
            BotToken = "test-token",
            WebhookSecretToken = "required-secret-token"
        });
        
        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, telegramOptionsWithSecret);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        // Don't add the secret token header.
        
        var json = """
        {
            "update_id": 123456,
            "message": {
                "message_id": 1,
                "date": 1705315800,
                "chat": {
                    "id": 12345,
                    "type": "group"
                },
                "from": {
                    "id": 67890,
                    "is_bot": false,
                    "first_name": "Test"
                },
                "text": "Test message"
            }
        }
        """;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act: Process the webhook request.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify Unauthorized is returned.
        Assert.IsType<UnauthorizedResult>(result);
        
        // Verify handler was never called.
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 2: Invalid Webhook Handling")]
    public async Task InvalidWebhookHandling_HandlerThrowsException_ReturnsOkToPreventRetries()
    {
        // Arrange: Configure handler to throw exception.
        _handlerMock
            .Setup(h => h.HandleUpdateAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated handler error"));
        
        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var json = """
        {
            "update_id": 123456,
            "message": {
                "message_id": 1,
                "date": 1705315800,
                "chat": {
                    "id": 12345,
                    "type": "group"
                },
                "from": {
                    "id": 67890,
                    "is_bot": false,
                    "first_name": "Test"
                },
                "text": "Test message"
            }
        }
        """;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act: Process the webhook request.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify OkResult is returned to prevent Telegram from retrying.
        Assert.IsType<OkResult>(result);
        
        // Verify error was logged.
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
