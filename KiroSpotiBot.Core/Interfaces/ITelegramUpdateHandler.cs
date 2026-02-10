using Telegram.Bot.Types;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Handles Telegram update processing.
/// </summary>
public interface ITelegramUpdateHandler
{
    /// <summary>
    /// Routes and processes a Telegram update.
    /// </summary>
    Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);
}
