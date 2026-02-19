using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Services;

/// <summary>
/// Service for handling auto-queue operations.
/// </summary>
public class AutoQueueService : IAutoQueueService
{
    private readonly ILogger<AutoQueueService> _logger;
    private readonly IUserGroupConfigRepository _userGroupConfigRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISpotifyService _spotifyService;

    public AutoQueueService(
        ILogger<AutoQueueService> logger,
        IUserGroupConfigRepository userGroupConfigRepository,
        IUserRepository userRepository,
        ISpotifyService spotifyService)
    {
        _logger = logger;
        _userGroupConfigRepository = userGroupConfigRepository;
        _userRepository = userRepository;
        _spotifyService = spotifyService;
    }

    /// <summary>
    /// Triggers auto-queue for all users with auto-queue enabled in a group chat.
    /// </summary>
    public async Task TriggerAutoQueueAsync(long telegramChatId, string trackId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all users with auto-queue enabled for this group chat.
            var usersWithAutoQueue = await _userGroupConfigRepository.GetUsersWithAutoQueueEnabledAsync(
                telegramChatId,
                cancellationToken);

            foreach (var userConfig in usersWithAutoQueue)
            {
                try
                {
                    var userId = long.Parse(userConfig.RowKey);

                    // Get user's access token.
                    var accessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(
                        userId,
                        cancellationToken);

                    if (string.IsNullOrWhiteSpace(accessToken))
                    {
                        _logger.LogDebug("User {UserId} has no valid access token for auto-queue.", userId);
                        continue;
                    }

                    // Check if user is currently playing music.
                    var isPlaying = await _spotifyService.IsUserPlayingAsync(accessToken, cancellationToken);

                    if (!isPlaying)
                    {
                        _logger.LogDebug("User {UserId} is not currently playing music. Skipping auto-queue.", userId);
                        continue;
                    }

                    // Add track to user's queue.
                    var success = await _spotifyService.AddTrackToQueueAsync(trackId, accessToken, cancellationToken);

                    if (success)
                    {
                        _logger.LogInformation("Successfully added track {TrackId} to queue for user {UserId}.", trackId, userId);
                    }
                    else
                    {
                        _logger.LogDebug("Failed to add track {TrackId} to queue for user {UserId}.", trackId, userId);
                    }
                }
                catch (Exception ex)
                {
                    // Handle failures silently - don't let one user's failure affect others.
                    _logger.LogDebug(ex, "Error adding track to queue for user {UserId}. Continuing with other users.", userConfig.RowKey);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering auto-queue for group {ChatId}.", telegramChatId);
        }
    }
}
