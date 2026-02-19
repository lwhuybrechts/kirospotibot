using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace KiroSpotiBot.Infrastructure.Handlers;

/// <summary>
/// Validates group chat configuration state and sends prompts if incomplete.
/// </summary>
public class GroupConfigurationValidator : IGroupConfigurationValidator
{
    private readonly ILogger<GroupConfigurationValidator> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IUserRepository _userRepository;

    public GroupConfigurationValidator(
        ILogger<GroupConfigurationValidator> logger,
        ITelegramBotClient telegramBotClient,
        IUserRepository userRepository)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Validates the configuration state of a group chat and sends prompts if incomplete.
    /// </summary>
    /// <returns>True if configuration is complete, false otherwise.</returns>
    public async Task<bool> ValidateAndPromptAsync(
        GroupChatEntity groupChat,
        long chatId,
        CancellationToken cancellationToken)
    {
        var missingConfiguration = new List<string>();

        // Check if administrator has authenticated with Spotify.
        var administrator = await _userRepository.GetByTelegramUserIdAsync(
            groupChat.AdministratorTelegramUserId,
            cancellationToken);

        var hasSpotifyAuth = administrator != null &&
                            !string.IsNullOrWhiteSpace(administrator.EncryptedAccessToken);

        if (!hasSpotifyAuth)
        {
            missingConfiguration.Add("🔐 Administrator needs to authenticate with Spotify using /auth");
        }

        // Check if playlist is configured.
        var hasPlaylist = !string.IsNullOrWhiteSpace(groupChat.PlaylistId);
        if (!hasPlaylist)
        {
            missingConfiguration.Add("🎵 Administrator needs to configure a playlist using /configure");
        }

        // If configuration is incomplete, send prompt and prevent track addition.
        if (missingConfiguration.Any())
        {
            var promptMessage = "⚠️ **Configuration Required**\n\n" +
                               "Before I can add tracks, the administrator must complete the setup:\n\n" +
                               string.Join("\n", missingConfiguration) +
                               "\n\nOnce configured, you can start sharing Spotify links!";

            await _telegramBotClient.SendMessage(
                chatId: chatId,
                text: promptMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Configuration incomplete for group {ChatId}. Sent prompt to administrator.",
                chatId);

            return false;
        }

        return true;
    }
}
