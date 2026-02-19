using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KiroSpotiBot.Infrastructure.Handlers;

/// <summary>
/// Handles group chat setup when bot is added.
/// </summary>
public class GroupSetupHandler : IGroupSetupHandler
{
    private readonly ILogger<GroupSetupHandler> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IGroupChatRepository _groupChatRepository;

    public GroupSetupHandler(
        ILogger<GroupSetupHandler> logger,
        ITelegramBotClient telegramBotClient,
        IGroupChatRepository groupChatRepository)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _groupChatRepository = groupChatRepository;
    }

    /// <summary>
    /// Handles bot being added to a group chat.
    /// </summary>
    public async Task HandleBotAddedToGroupAsync(ChatMemberUpdated update, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Bot was added to group {ChatId} by user {UserId}.",
                update.Chat.Id, update.From.Id);

            // Create group chat record in Table Storage.
            var groupChat = await _groupChatRepository.CreateGroupChatAsync(
                update.Chat.Id,
                update.From.Id,
                cancellationToken);

            _logger.LogInformation("Created group chat record for {ChatId} with administrator {UserId}.",
                groupChat.TelegramChatId, groupChat.AdministratorTelegramUserId);

            // Send welcome message explaining administrator privileges.
            var welcomeMessage = "👋 Hello! I'm KiroSpotiBot. I'll help you build collaborative Spotify playlists.\n\n" +
                                "🎵 **How it works:**\n" +
                                "• Share Spotify track links in this chat\n" +
                                "• I'll automatically add them to your configured playlist\n" +
                                "• Vote on tracks with 👍 or 👎 reactions\n" +
                                "• Tracks with too many downvotes are removed automatically\n\n" +
                                $"👤 **Administrator:** <a href=\"tg://user?id={update.From.Id}\">{update.From.FirstName}</a>\n" +
                                "The administrator can configure the bot using these commands:\n" +
                                "• /auth - Authenticate with Spotify\n" +
                                "• /configure - Set the target playlist\n" +
                                "• /threshold - Set downvote threshold\n\n" +
                                "⚙️ **Setup required:**\n" +
                                "1. Administrator must authenticate with Spotify using /auth\n" +
                                "2. Administrator must configure a playlist using /configure\n\n" +
                                "Once configured, start sharing Spotify links!";

            await _telegramBotClient.SendMessage(
                chatId: update.Chat.Id,
                text: welcomeMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Sent welcome message to group {ChatId}.", update.Chat.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling bot added to group {ChatId}.", update.Chat.Id);
            throw;
        }
    }
}
