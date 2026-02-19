using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KiroSpotiBot.Infrastructure.Handlers;

/// <summary>
/// Handles Telegram bot commands.
/// </summary>
public class CommandHandler : ICommandHandler
{
    private readonly ILogger<CommandHandler> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly ISpotifyOAuthHandler _oauthHandler;
    private readonly IUserRepository _userRepository;
    private readonly IGroupChatRepository _groupChatRepository;
    private readonly IUserGroupConfigRepository _userGroupConfigRepository;
    private readonly ISpotifyService _spotifyService;

    public CommandHandler(
        ILogger<CommandHandler> logger,
        ITelegramBotClient telegramBotClient,
        ISpotifyOAuthHandler oauthHandler,
        IUserRepository userRepository,
        IGroupChatRepository groupChatRepository,
        IUserGroupConfigRepository userGroupConfigRepository,
        ISpotifyService spotifyService)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _oauthHandler = oauthHandler;
        _userRepository = userRepository;
        _groupChatRepository = groupChatRepository;
        _userGroupConfigRepository = userGroupConfigRepository;
        _spotifyService = spotifyService;
    }

    /// <inheritdoc/>
    public async Task HandleAuthCommandAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure user exists in database.
            var user = await _userRepository.GetByTelegramUserIdAsync(message.From!.Id, cancellationToken);
            if (user == null)
            {
                _logger.LogInformation("Creating new user record for Telegram user {UserId}.", message.From.Id);
                user = await _userRepository.CreateUserAsync(message.From.Id, cancellationToken);
            }

            // Check if user is already authenticated.
            if (!string.IsNullOrWhiteSpace(user.EncryptedAccessToken))
            {
                _logger.LogInformation("User {UserId} is already authenticated with Spotify.", message.From.Id);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.From.Id,
                    text: "✅ You are already authenticated with Spotify! If you need to re-authenticate, please contact support.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Start OAuth flow.
            var authUrl = await _oauthHandler.StartAuthAsync(message.From.Id, message.From.Id, cancellationToken);

            _logger.LogInformation("Sending OAuth link to user {UserId} in private chat.", message.From.Id);

            // Send OAuth link in private chat.
            await _telegramBotClient.SendMessage(
                chatId: message.From.Id,
                text: $"🔐 Click the link below to authenticate with Spotify:\n\n{authUrl}\n\nThis link will expire in 10 minutes.",
                cancellationToken: cancellationToken);

            // If command was sent in a group, acknowledge in the group.
            if (message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
            {
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"✅ @{message.From.Username ?? message.From.FirstName}, I've sent you an authentication link in a private message.",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /auth command from user {UserId}.", message.From?.Id);
            
            await _telegramBotClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ An error occurred while processing your authentication request. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task HandleConfigureCommandAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            // Only allow in group chats.
            if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
            {
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ The /configure command can only be used in group chats.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Get group chat configuration.
            var groupChat = await _groupChatRepository.GetByTelegramChatIdAsync(message.Chat.Id, cancellationToken);
            if (groupChat == null)
            {
                _logger.LogWarning("No group chat configuration found for chat {ChatId}.", message.Chat.Id);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ This group is not configured yet. Please wait for the administrator to set up the bot.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Check if user is the administrator.
            if (message.From!.Id != groupChat.AdministratorTelegramUserId)
            {
                _logger.LogWarning("User {UserId} attempted to configure group {ChatId} but is not the administrator.", 
                    message.From.Id, message.Chat.Id);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Only the group administrator can configure the playlist.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Get administrator's credentials.
            var admin = await _userRepository.GetByTelegramUserIdAsync(groupChat.AdministratorTelegramUserId, cancellationToken);
            if (admin == null || string.IsNullOrWhiteSpace(admin.EncryptedAccessToken))
            {
                _logger.LogWarning("Administrator {UserId} is not authenticated with Spotify.", groupChat.AdministratorTelegramUserId);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ You must authenticate with Spotify first. Use /auth to get started.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Parse playlist ID from command.
            var parts = message.Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Please provide a playlist ID. Usage: /configure <playlist_id>",
                    cancellationToken: cancellationToken);
                return;
            }

            var playlistId = parts[1].Trim();

            // Validate playlist exists and is accessible.
            var accessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(
                groupChat.AdministratorTelegramUserId, 
                cancellationToken);
            
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogWarning("Failed to decrypt access token for administrator {UserId}.", 
                    groupChat.AdministratorTelegramUserId);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Authentication error. Please re-authenticate using /auth.",
                    cancellationToken: cancellationToken);
                return;
            }
            
            var isValid = await _spotifyService.ValidatePlaylistAsync(playlistId, accessToken, cancellationToken);
            
            if (!isValid)
            {
                _logger.LogWarning("Playlist {PlaylistId} is not valid or accessible for user {UserId}.", 
                    playlistId, groupChat.AdministratorTelegramUserId);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ The playlist is not found or you don't have access to it. Please check the playlist ID and try again.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Check if playlist is already linked to another group.
            var isLinked = await _groupChatRepository.IsPlaylistLinkedAsync(playlistId, cancellationToken);
            if (isLinked && groupChat.PlaylistId != playlistId)
            {
                _logger.LogWarning("Playlist {PlaylistId} is already linked to another group.", playlistId);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ This playlist is already linked to another group chat. Each playlist can only be used by one group.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Get playlist name.
            var playlistName = await _spotifyService.GetPlaylistNameAsync(playlistId, accessToken, cancellationToken);

            // Update group chat configuration.
            groupChat.PlaylistId = playlistId;
            groupChat.PlaylistName = playlistName ?? "Unknown Playlist";
            await _groupChatRepository.UpdateGroupChatAsync(groupChat, cancellationToken);

            _logger.LogInformation("Updated playlist configuration for group {ChatId} to playlist {PlaylistId}.", 
                message.Chat.Id, playlistId);

            await _telegramBotClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"✅ Playlist configured successfully!\n\nPlaylist: {groupChat.PlaylistName}\n\nYou can now share Spotify track links in this group.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /configure command in chat {ChatId}.", message.Chat.Id);
            
            await _telegramBotClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ An error occurred while configuring the playlist. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task HandleThresholdCommandAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            // Only allow in group chats.
            if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
            {
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ The /threshold command can only be used in group chats.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Get group chat configuration.
            var groupChat = await _groupChatRepository.GetByTelegramChatIdAsync(message.Chat.Id, cancellationToken);
            if (groupChat == null)
            {
                _logger.LogWarning("No group chat configuration found for chat {ChatId}.", message.Chat.Id);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ This group is not configured yet. Please wait for the administrator to set up the bot.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Check if user is the administrator.
            if (message.From!.Id != groupChat.AdministratorTelegramUserId)
            {
                _logger.LogWarning("User {UserId} attempted to set threshold for group {ChatId} but is not the administrator.", 
                    message.From.Id, message.Chat.Id);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Only the group administrator can change the downvote threshold.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Parse threshold from command.
            var parts = message.Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"❌ Please provide a threshold value. Usage: /threshold <number>\n\nCurrent threshold: {groupChat.DownvoteThreshold}",
                    cancellationToken: cancellationToken);
                return;
            }

            if (!int.TryParse(parts[1], out var threshold) || threshold <= 0)
            {
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ The threshold must be a positive integer.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Update threshold.
            groupChat.DownvoteThreshold = threshold;
            await _groupChatRepository.UpdateGroupChatAsync(groupChat, cancellationToken);

            _logger.LogInformation("Updated downvote threshold for group {ChatId} to {Threshold}.", 
                message.Chat.Id, threshold);

            await _telegramBotClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"✅ Downvote threshold updated to {threshold}.\n\nTracks will be automatically removed when they reach {threshold} downvote(s).",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /threshold command in chat {ChatId}.", message.Chat.Id);
            
            await _telegramBotClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ An error occurred while updating the threshold. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task HandleAutoQueueCommandAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            // Only allow in group chats.
            if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
            {
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ The /autoqueue command can only be used in group chats.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Ensure user exists in database.
            var user = await _userRepository.GetByTelegramUserIdAsync(message.From!.Id, cancellationToken);
            if (user == null)
            {
                _logger.LogInformation("Creating new user record for Telegram user {UserId}.", message.From.Id);
                user = await _userRepository.CreateUserAsync(message.From.Id, cancellationToken);
            }

            // Check if user is authenticated.
            if (string.IsNullOrWhiteSpace(user.EncryptedAccessToken))
            {
                _logger.LogInformation("User {UserId} attempted to enable auto-queue but is not authenticated.", message.From.Id);
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ You must authenticate with Spotify before enabling auto-queue. Use /auth to get started.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Parse command argument.
            var parts = message.Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                // Get current status.
                var currentConfig = await _userGroupConfigRepository.GetAsync(message.Chat.Id, message.From.Id, cancellationToken);
                var currentStatus = currentConfig?.AutoQueueEnabled ?? false;
                
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"ℹ️ Auto-queue is currently {(currentStatus ? "enabled" : "disabled")} for you in this group.\n\n" +
                          "Usage: /autoqueue <on|off>",
                    cancellationToken: cancellationToken);
                return;
            }

            var action = parts[1].ToLowerInvariant();
            bool enableAutoQueue;

            if (action == "on" || action == "enable" || action == "true")
            {
                enableAutoQueue = true;
            }
            else if (action == "off" || action == "disable" || action == "false")
            {
                enableAutoQueue = false;
            }
            else
            {
                await _telegramBotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Invalid argument. Usage: /autoqueue <on|off>",
                    cancellationToken: cancellationToken);
                return;
            }

            // Get or create user group config.
            var config = await _userGroupConfigRepository.GetAsync(message.Chat.Id, message.From.Id, cancellationToken);
            if (config == null)
            {
                config = new UserGroupConfigEntity(message.Chat.Id, message.From.Id);
            }

            config.AutoQueueEnabled = enableAutoQueue;
            await _userGroupConfigRepository.UpsertAsync(config, cancellationToken);

            _logger.LogInformation("User {UserId} {Action} auto-queue for group {ChatId}.", 
                message.From.Id, enableAutoQueue ? "enabled" : "disabled", message.Chat.Id);

            await _telegramBotClient.SendMessage(
                chatId: message.Chat.Id,
                text: enableAutoQueue 
                    ? "✅ Auto-queue enabled! Tracks added to this group's playlist will automatically be added to your Spotify queue when you're playing music."
                    : "✅ Auto-queue disabled. Tracks will no longer be automatically added to your Spotify queue.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /autoqueue command from user {UserId} in chat {ChatId}.", 
                message.From?.Id, message.Chat.Id);
            
            await _telegramBotClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ An error occurred while updating your auto-queue preference. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }
}
