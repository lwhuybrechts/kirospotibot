using Telegram.Bot.Types;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for handling group chat setup when bot is added.
/// </summary>
public interface IGroupSetupHandler
{
    /// <summary>
    /// Handles bot being added to a group chat.
    /// </summary>
    /// <param name="update">The chat member update event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleBotAddedToGroupAsync(ChatMemberUpdated update, CancellationToken cancellationToken);
}
