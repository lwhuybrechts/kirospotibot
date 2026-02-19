using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Functions;
using KiroSpotiBot.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for webhook payload parsing validation.
/// Property 1: Webhook Payload Parsing
/// Validates: Requirements 1.2
/// 
/// For any valid Telegram webhook payload, extracting the message text and chat identifier
/// should successfully return the correct values from the payload structure.
/// 
/// Note: These tests use xUnit's Theory attribute with InlineData to simulate
/// property-based testing behavior by testing multiple input combinations.
/// </summary>
public class WebhookPayloadParsingPropertyTests
{
    private readonly Mock<ILogger<TelegramWebhookFunction>> _loggerMock;
    private readonly Mock<ITelegramUpdateHandler> _handlerMock;
    private readonly IOptions<TelegramOptions> _telegramOptions;

    public WebhookPayloadParsingPropertyTests()
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
    [InlineData(12345, 67890, "Check out this track: https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(11111, 22222, "Hello world")]
    [InlineData(99999, 88888, "Multiple messages with text")]
    [InlineData(54321, 98765, "")]
    [InlineData(77777, 66666, "Special characters: !@#$%^&*()")]
    [InlineData(10001, 20002, "Unicode: 你好世界 🎵")]
    [InlineData(30003, 40004, "Long message with lots of text that goes on and on and on")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 1: Webhook Payload Parsing")]
    public async Task WebhookPayloadParsing_ValidUpdate_ExtractsMessageTextAndChatId(
        long chatId,
        long userId,
        string messageText)
    {
        // Arrange: Create a valid Telegram Update JSON payload.
        var json = $$"""
        {
            "update_id": 123456,
            "message": {
                "message_id": 1,
                "date": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
                "chat": {
                    "id": {{chatId}},
                    "type": "group"
                },
                "from": {
                    "id": {{userId}},
                    "is_bot": false,
                    "first_name": "Test",
                    "username": "testuser"
                },
                "text": {{System.Text.Json.JsonSerializer.Serialize(messageText)}}
            }
        }
        """;

        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act: Process the webhook request.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify the handler was called with the correct update.
        Assert.IsType<OkResult>(result);
        
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(
                It.Is<Update>(u => 
                    u.Message != null &&
                    u.Message.Chat.Id == chatId &&
                    u.Message.From!.Id == userId &&
                    u.Message.Text == messageText),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, 67890)]
    [InlineData(11111, 22222)]
    [InlineData(99999, 88888)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 1: Webhook Payload Parsing")]
    public async Task WebhookPayloadParsing_EditedMessage_ExtractsCorrectData(
        long chatId,
        long userId)
    {
        // Arrange: Create update JSON with edited message.
        // Note: Telegram API uses snake_case for JSON properties.
        // EditedMessage requires edit_date field.
        var json = $$"""
        {
            "update_id": 123456,
            "edited_message": {
                "message_id": 1,
                "date": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
                "edit_date": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
                "chat": {
                    "id": {{chatId}},
                    "type": "group"
                },
                "from": {
                    "id": {{userId}},
                    "is_bot": false,
                    "first_name": "Test"
                },
                "text": "Edited text"
            }
        }
        """;

        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act.
        var result = await function.Run(request, CancellationToken.None);

        // Assert.
        Assert.IsType<OkResult>(result);
        
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(
                It.Is<Update>(u => 
                    u.EditedMessage != null &&
                    u.EditedMessage.Chat.Id == chatId &&
                    u.EditedMessage.From!.Id == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, 67890, "callback_data_1")]
    [InlineData(11111, 22222, "vote_upvote_track123")]
    [InlineData(99999, 88888, "queue_add")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 1: Webhook Payload Parsing")]
    public async Task WebhookPayloadParsing_CallbackQuery_ExtractsCorrectData(
        long chatId,
        long userId,
        string callbackData)
    {
        // Arrange: Create update JSON with callback query.
        // CallbackQuery requires: id, from, chat_instance, and either data or game_short_name.
        var json = $$"""
        {
            "update_id": 123456,
            "callback_query": {
                "id": "callback123",
                "from": {
                    "id": {{userId}},
                    "is_bot": false,
                    "first_name": "Test"
                },
                "message": {
                    "message_id": 1,
                    "date": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
                    "chat": {
                        "id": {{chatId}},
                        "type": "group"
                    },
                    "from": {
                        "id": 999999,
                        "is_bot": true,
                        "first_name": "Bot"
                    },
                    "text": "Original message"
                },
                "chat_instance": "chat_instance_123",
                "data": {{System.Text.Json.JsonSerializer.Serialize(callbackData)}}
            }
        }
        """;

        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act.
        var result = await function.Run(request, CancellationToken.None);

        // Assert.
        Assert.IsType<OkResult>(result);
        
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(
                It.Is<Update>(u => 
                    u.CallbackQuery != null &&
                    u.CallbackQuery.From.Id == userId &&
                    u.CallbackQuery.Data == callbackData),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, 67890, 1)]
    [InlineData(11111, 22222, 2)]
    [InlineData(99999, 88888, 5)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 1: Webhook Payload Parsing")]
    public async Task WebhookPayloadParsing_MessageReaction_ExtractsCorrectData(
        long chatId,
        long userId,
        int messageId)
    {
        // Arrange: Create update JSON with message reaction.
        // MessageReaction requires: chat, message_id, date, old_reaction, new_reaction.
        var json = $$"""
        {
            "update_id": 123456,
            "message_reaction": {
                "chat": {
                    "id": {{chatId}},
                    "type": "group"
                },
                "user": {
                    "id": {{userId}},
                    "is_bot": false,
                    "first_name": "Test"
                },
                "message_id": {{messageId}},
                "date": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
                "old_reaction": [],
                "new_reaction": [
                    {
                        "type": "emoji",
                        "emoji": "👍"
                    }
                ]
            }
        }
        """;

        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act.
        var result = await function.Run(request, CancellationToken.None);

        // Assert.
        Assert.IsType<OkResult>(result);
        
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(
                It.Is<Update>(u => 
                    u.MessageReaction != null &&
                    u.MessageReaction.Chat.Id == chatId &&
                    u.MessageReaction.User!.Id == userId &&
                    u.MessageReaction.MessageId == messageId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, 67890)]
    [InlineData(11111, 22222)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 1: Webhook Payload Parsing")]
    public async Task WebhookPayloadParsing_MyChatMember_ExtractsCorrectData(
        long chatId,
        long userId)
    {
        // Arrange: Create update JSON with my_chat_member (bot added to group).
        // ChatMemberUpdated requires: chat, from, date, old_chat_member, new_chat_member.
        var json = $$"""
        {
            "update_id": 123456,
            "my_chat_member": {
                "chat": {
                    "id": {{chatId}},
                    "type": "group"
                },
                "from": {
                    "id": {{userId}},
                    "is_bot": false,
                    "first_name": "Test"
                },
                "date": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
                "old_chat_member": {
                    "user": {
                        "id": 999999,
                        "is_bot": true,
                        "first_name": "Bot"
                    },
                    "status": "left"
                },
                "new_chat_member": {
                    "user": {
                        "id": 999999,
                        "is_bot": true,
                        "first_name": "Bot"
                    },
                    "status": "member"
                }
            }
        }
        """;

        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act.
        var result = await function.Run(request, CancellationToken.None);

        // Assert.
        Assert.IsType<OkResult>(result);
        
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(
                It.Is<Update>(u => 
                    u.MyChatMember != null &&
                    u.MyChatMember.Chat.Id == chatId &&
                    u.MyChatMember.From.Id == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(12345, 67890, "https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp", "spotify:track:3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(11111, 22222, "Check this: https://play.spotify.com/track/5Z01UMMf7V1o0MzF86s6WJ", "Another: spotify:track:5Z01UMMf7V1o0MzF86s6WJ")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 1: Webhook Payload Parsing")]
    public async Task WebhookPayloadParsing_MessageWithMultipleFormats_PreservesAllContent(
        long chatId,
        long userId,
        string url1,
        string url2)
    {
        // Arrange: Create message JSON with multiple Spotify URL formats.
        var messageText = $"Check these tracks: {url1} and {url2}";
        var json = $$"""
        {
            "update_id": 123456,
            "message": {
                "message_id": 1,
                "date": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
                "chat": {
                    "id": {{chatId}},
                    "type": "group"
                },
                "from": {
                    "id": {{userId}},
                    "is_bot": false,
                    "first_name": "Test"
                },
                "text": {{System.Text.Json.JsonSerializer.Serialize(messageText)}}
            }
        }
        """;

        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify the full message text is preserved.
        Assert.IsType<OkResult>(result);
        
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(
                It.Is<Update>(u => 
                    u.Message != null &&
                    u.Message.Text == messageText &&
                    u.Message.Text!.Contains(url1) &&
                    u.Message.Text.Contains(url2)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 1: Webhook Payload Parsing")]
    public async Task WebhookPayloadParsing_ComplexUpdate_ExtractsAllFields()
    {
        // Arrange: Create a complex update JSON with multiple fields.
        var json = """
        {
            "update_id": 987654,
            "message": {
                "message_id": 42,
                "date": 1705315800,
                "chat": {
                    "id": -1001234567890,
                    "type": "supergroup",
                    "title": "Test Group"
                },
                "from": {
                    "id": 123456789,
                    "is_bot": false,
                    "first_name": "John",
                    "last_name": "Doe",
                    "username": "johndoe"
                },
                "text": "Complex message with @mentions and #hashtags"
            }
        }
        """;

        var function = new TelegramWebhookFunction(_loggerMock.Object, _handlerMock.Object, _telegramOptions);
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentType = "application/json";

        // Act.
        var result = await function.Run(request, CancellationToken.None);

        // Assert: Verify all fields are correctly extracted.
        Assert.IsType<OkResult>(result);
        
        _handlerMock.Verify(
            h => h.HandleUpdateAsync(
                It.Is<Update>(u => 
                    u.Id == 987654 &&
                    u.Message != null &&
                    u.Message.MessageId == 42 &&
                    u.Message.Chat.Id == -1001234567890 &&
                    u.Message.Chat.Type == ChatType.Supergroup &&
                    u.Message.From!.Id == 123456789 &&
                    u.Message.From.Username == "johndoe" &&
                    u.Message.Text == "Complex message with @mentions and #hashtags"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
