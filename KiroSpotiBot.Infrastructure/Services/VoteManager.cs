using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Services;

/// <summary>
/// Service for managing track voting logic and automatic removal.
/// </summary>
public class VoteManager : IVoteManager
{
    private readonly IVoteRepository _voteRepository;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly IGroupChatRepository _groupChatRepository;
    private readonly ISpotifyService _spotifyService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<VoteManager> _logger;

    public VoteManager(
        IVoteRepository voteRepository,
        ITrackRecordRepository trackRecordRepository,
        IGroupChatRepository groupChatRepository,
        ISpotifyService spotifyService,
        IUserRepository userRepository,
        ILogger<VoteManager> logger)
    {
        _voteRepository = voteRepository;
        _trackRecordRepository = trackRecordRepository;
        _groupChatRepository = groupChatRepository;
        _spotifyService = spotifyService;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> RecordVoteAsync(
        string trackRecordId,
        long telegramChatId,
        long telegramUserId,
        string voteType,
        string voterUsername,
        string? voterAvatarUrl,
        CancellationToken cancellationToken = default)
    {
        // Get the track record.
        var trackRecord = await _trackRecordRepository.GetByIdAsync(trackRecordId, telegramChatId, cancellationToken);
        if (trackRecord == null)
        {
            _logger.LogWarning("Track record {TrackRecordId} not found for chat {ChatId}.", trackRecordId, telegramChatId);
            return false;
        }

        // Prevent voting on deleted tracks.
        if (trackRecord.IsDeleted)
        {
            _logger.LogInformation("Attempted to vote on deleted track {TrackRecordId}.", trackRecordId);
            return false;
        }

        // Check if user already voted.
        var existingVote = await _voteRepository.GetVoteAsync(trackRecordId, telegramUserId, cancellationToken);

        if (existingVote != null)
        {
            // Update existing vote if type changed.
            if (existingVote.VoteType != voteType)
            {
                existingVote.VoteType = voteType;
                existingVote.UpdatedAt = DateTime.UtcNow;
                await _voteRepository.UpsertVoteAsync(existingVote, cancellationToken);
                _logger.LogInformation("Updated vote for user {UserId} on track {TrackRecordId} to {VoteType}.", 
                    telegramUserId, trackRecordId, voteType);
            }
        }
        else
        {
            // Create new vote.
            var vote = new VoteEntity(trackRecordId, telegramUserId, voteType)
            {
                VoterUsername = voterUsername,
                VoterAvatarUrl = voterAvatarUrl
            };
            await _voteRepository.UpsertVoteAsync(vote, cancellationToken);
            _logger.LogInformation("Created new vote for user {UserId} on track {TrackRecordId} as {VoteType}.", 
                telegramUserId, trackRecordId, voteType);
        }

        // Update denormalized vote counts in track record.
        var voteCounts = await _voteRepository.GetVoteCountsAsync(trackRecordId, cancellationToken);
        trackRecord.UpvoteCount = voteCounts.upvotes;
        trackRecord.DownvoteCount = voteCounts.downvotes;
        await _trackRecordRepository.UpdateTrackRecordAsync(trackRecord, cancellationToken);

        // Check if track should be removed based on threshold.
        var groupChat = await _groupChatRepository.GetByTelegramChatIdAsync(telegramChatId, cancellationToken);
        if (groupChat == null)
        {
            _logger.LogWarning("Group chat {ChatId} not found.", telegramChatId);
            return false;
        }

        if (trackRecord.DownvoteCount >= groupChat.DownvoteThreshold)
        {
            _logger.LogInformation("Track {TrackRecordId} reached downvote threshold ({Count}/{Threshold}). Removing from playlist.", 
                trackRecordId, trackRecord.DownvoteCount, groupChat.DownvoteThreshold);

            // Get administrator credentials.
            var admin = await _userRepository.GetByTelegramUserIdAsync(groupChat.AdministratorTelegramUserId, cancellationToken);
            if (admin == null)
            {
                _logger.LogError("Administrator {AdminId} not found for chat {ChatId}.", 
                    groupChat.AdministratorTelegramUserId, telegramChatId);
                return false;
            }

            var accessToken = await _userRepository.GetDecryptedSpotifyAccessTokenAsync(groupChat.AdministratorTelegramUserId, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Administrator {AdminId} not authenticated for chat {ChatId}.", 
                    groupChat.AdministratorTelegramUserId, telegramChatId);
                return false;
            }

            if (string.IsNullOrEmpty(groupChat.PlaylistId))
            {
                _logger.LogError("Group chat {ChatId} has no playlist configured.", telegramChatId);
                return false;
            }

            // Remove track from Spotify playlist.
            var removed = await _spotifyService.RemoveTrackFromPlaylistAsync(
                groupChat.PlaylistId, 
                trackRecord.TrackSpotifyId, 
                accessToken, 
                cancellationToken);

            if (removed)
            {
                // Mark track as deleted.
                await _trackRecordRepository.MarkTrackAsDeletedAsync(trackRecordId, telegramChatId, cancellationToken);
                _logger.LogInformation("Successfully removed track {TrackRecordId} from playlist {PlaylistId}.", 
                    trackRecordId, groupChat.PlaylistId);
                return true;
            }
            else
            {
                _logger.LogError("Failed to remove track {TrackRecordId} from playlist {PlaylistId}.", 
                    trackRecordId, groupChat.PlaylistId);
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task RemoveVoteAsync(
        string trackRecordId,
        long telegramChatId,
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        // Delete the vote.
        await _voteRepository.DeleteVoteAsync(trackRecordId, telegramUserId, cancellationToken);
        _logger.LogInformation("Removed vote for user {UserId} on track {TrackRecordId}.", telegramUserId, trackRecordId);

        // Update denormalized vote counts in track record.
        var trackRecord = await _trackRecordRepository.GetByIdAsync(trackRecordId, telegramChatId, cancellationToken);
        if (trackRecord != null)
        {
            var voteCounts = await _voteRepository.GetVoteCountsAsync(trackRecordId, cancellationToken);
            trackRecord.UpvoteCount = voteCounts.upvotes;
            trackRecord.DownvoteCount = voteCounts.downvotes;
            await _trackRecordRepository.UpdateTrackRecordAsync(trackRecord, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<(int upvotes, int downvotes)> GetVoteCountsAsync(
        string trackRecordId,
        CancellationToken cancellationToken = default)
    {
        return await _voteRepository.GetVoteCountsAsync(trackRecordId, cancellationToken);
    }
}
