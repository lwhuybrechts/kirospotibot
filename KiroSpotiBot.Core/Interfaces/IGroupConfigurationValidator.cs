using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for validating group chat configuration state.
/// </summary>
public interface IGroupConfigurationValidator
{
    /// <summary>
    /// Validates the configuration state of a group chat and sends prompts if incomplete.
    /// </summary>
    /// <param name="groupChat">The group chat entity to validate.</param>
    /// <param name="chatId">The Telegram chat ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration is complete, false otherwise.</returns>
    Task<bool> ValidateAndPromptAsync(
        GroupChatEntity groupChat,
        long chatId,
        CancellationToken cancellationToken);
}
