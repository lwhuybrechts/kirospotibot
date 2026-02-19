using System.Text.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KiroSpotiBot.Tests.Helpers;

/// <summary>
/// Helper class for creating Telegram Message objects in tests.
/// Uses JSON deserialization to create objects with non-virtual properties.
/// </summary>
public static class MessageHelper
{
    /// <summary>
    /// Creates a Message object with the specified properties.
    /// </summary>
    public static Message CreateMessage(
        long messageId,
        User from,
        Chat chat,
        DateTime date,
        string? text = null)
    {
        // Build JSON parts separately to avoid interpolation issues.
        var usernameJson = from.Username != null ? $", \"username\": \"{from.Username}\"" : "";
        var firstNameJson = from.FirstName != null ? $", \"first_name\": \"{from.FirstName}\"" : "";
        var textJson = text != null ? $", \"text\": \"{text}\"" : "";
        
        // Create JSON representation and deserialize to bypass property restrictions.
        var json = $@"{{
            ""message_id"": {messageId},
            ""from"": {{
                ""id"": {from.Id},
                ""is_bot"": {(from.IsBot ? "true" : "false")}{usernameJson}{firstNameJson}
            }},
            ""chat"": {{
                ""id"": {chat.Id},
                ""type"": ""{GetChatTypeString(chat.Type)}""
            }},
            ""date"": {new DateTimeOffset(date).ToUnixTimeSeconds()}{textJson}
        }}";
        
        // Use Telegram.Bot's JsonBotAPI.Options for correct snake_case deserialization.
        var message = JsonSerializer.Deserialize<Message>(json, Telegram.Bot.JsonBotAPI.Options);
        
        if (message == null)
        {
            throw new InvalidOperationException("Failed to create Message instance.");
        }
        
        return message;
    }
    
    /// <summary>
    /// Creates a User object with the specified properties.
    /// </summary>
    public static User CreateUser(long id, string? username = null, string? firstName = null)
    {
        var usernameJson = username != null ? $", \"username\": \"{username}\"" : "";
        var firstNameJson = firstName != null ? $", \"first_name\": \"{firstName}\"" : "";
        
        var json = $@"{{
            ""id"": {id},
            ""is_bot"": false{usernameJson}{firstNameJson}
        }}";
        
        // Use Telegram.Bot's JsonBotAPI.Options for correct snake_case deserialization.
        var user = JsonSerializer.Deserialize<User>(json, Telegram.Bot.JsonBotAPI.Options);
        
        if (user == null)
        {
            throw new InvalidOperationException("Failed to create User instance.");
        }
        
        return user;
    }
    
    /// <summary>
    /// Creates a Chat object with the specified properties.
    /// </summary>
    public static Chat CreateChat(long id, ChatType type)
    {
        var json = $@"{{
            ""id"": {id},
            ""type"": ""{GetChatTypeString(type)}""
        }}";
        
        // Use Telegram.Bot's JsonBotAPI.Options for correct snake_case deserialization.
        var chat = JsonSerializer.Deserialize<Chat>(json, Telegram.Bot.JsonBotAPI.Options);
        
        if (chat == null)
        {
            throw new InvalidOperationException("Failed to create Chat instance.");
        }
        
        return chat;
    }
    
    private static string GetChatTypeString(ChatType type)
    {
        return type switch
        {
            ChatType.Private => "private",
            ChatType.Group => "group",
            ChatType.Supergroup => "supergroup",
            ChatType.Channel => "channel",
            _ => "group"
        };
    }
}
