using System.ComponentModel.DataAnnotations;

namespace KiroSpotiBot.Infrastructure.Options;

/// <summary>
/// Configuration options for Telegram Bot integration.
/// </summary>
public class TelegramOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Telegram";

    /// <summary>
    /// The Telegram bot token.
    /// </summary>
    [Required(ErrorMessage = "Telegram:BotToken is required.")]
    public string BotToken { get; set; } = string.Empty;
}
