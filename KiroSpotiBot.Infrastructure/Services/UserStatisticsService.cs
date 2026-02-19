using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;

namespace KiroSpotiBot.Infrastructure.Services;

/// <summary>
/// Service for calculating user statistics across repositories.
/// </summary>
public class UserStatisticsService : IUserStatisticsService
{
    private readonly IUserRepository _userRepository;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly IGroupChatRepository _groupChatRepository;

    public UserStatisticsService(
        IUserRepository userRepository,
        ITrackRecordRepository trackRecordRepository,
        IVoteRepository voteRepository,
        IGroupChatRepository groupChatRepository)
    {
        _userRepository = userRepository;
        _trackRecordRepository = trackRecordRepository;
        _voteRepository = voteRepository;
        _groupChatRepository = groupChatRepository;
    }

    public async Task<UserDetailStatistics?> GetUserDetailStatisticsAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        var tracksShared = await _trackRecordRepository.GetTrackCountByUserAsync(telegramUserId, cancellationToken);
        var upvotesGiven = await _voteRepository.GetTotalUpvotesGivenByUserAsync(telegramUserId, cancellationToken);
        var downvotesGiven = await _voteRepository.GetTotalDownvotesGivenByUserAsync(telegramUserId, cancellationToken);
        var (upvotesReceived, downvotesReceived) = await GetVotesReceivedByUserAsync(telegramUserId, cancellationToken);

        // Get username from track records.
        var userTracks = await _trackRecordRepository.GetByUserAsync(telegramUserId, 0, 1, cancellationToken);
        var firstTrack = userTracks.FirstOrDefault();

        // Calculate per-playlist statistics.
        var playlistStats = await GetPlaylistStatisticsAsync(telegramUserId, cancellationToken);

        // Get voters information.
        var (upvoters, downvoters) = await GetVotersInfoAsync(telegramUserId, cancellationToken);

        return new UserDetailStatistics
        {
            TelegramUserId = telegramUserId,
            Username = firstTrack?.SharedByUsername ?? $"User {telegramUserId}",
            AvatarUrl = user.TelegramAvatarUrl ?? firstTrack?.SharedByAvatarUrl,
            TotalTracksShared = tracksShared,
            TotalUpvotesGiven = upvotesGiven,
            TotalDownvotesGiven = downvotesGiven,
            TotalUpvotesReceived = upvotesReceived,
            TotalDownvotesReceived = downvotesReceived,
            PlaylistStats = playlistStats,
            Upvoters = upvoters,
            Downvoters = downvoters
        };
    }

    private async Task<(int upvotes, int downvotes)> GetVotesReceivedByUserAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        // Get all tracks shared by the user across all playlists.
        var filter = $"SharedByTelegramUserId eq {telegramUserId}L";
        var userTracks = await _trackRecordRepository.QueryAsync(filter, cancellationToken);

        int totalUpvotes = 0;
        int totalDownvotes = 0;

        foreach (var track in userTracks.Where(t => !t.IsDeleted))
        {
            totalUpvotes += track.UpvoteCount;
            totalDownvotes += track.DownvoteCount;
        }

        return (totalUpvotes, totalDownvotes);
    }

    private async Task<List<PlaylistStatistics>> GetPlaylistStatisticsAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        var groupChats = await _groupChatRepository.GetAllWithPlaylistsAsync(cancellationToken);
        var playlistStats = new List<PlaylistStatistics>();

        foreach (var groupChat in groupChats)
        {
            var chatTracks = await _trackRecordRepository.GetByGroupChatAsync(groupChat.TelegramChatId, 0, int.MaxValue, cancellationToken);
            var userTracksInChat = chatTracks.Where(t => t.SharedByTelegramUserId == telegramUserId && !t.IsDeleted).ToList();

            if (userTracksInChat.Any())
            {
                var upvotesReceived = userTracksInChat.Sum(t => t.UpvoteCount);
                var downvotesReceived = userTracksInChat.Sum(t => t.DownvoteCount);

                // Calculate votes given in this playlist.
                var upvotesGiven = 0;
                var downvotesGiven = 0;

                foreach (var track in chatTracks)
                {
                    var vote = await _voteRepository.GetVoteAsync(track.TrackRecordId, telegramUserId, cancellationToken);
                    if (vote != null)
                    {
                        if (vote.VoteType == "Upvote")
                            upvotesGiven++;
                        else if (vote.VoteType == "Downvote")
                            downvotesGiven++;
                    }
                }

                playlistStats.Add(new PlaylistStatistics
                {
                    TelegramChatId = groupChat.TelegramChatId,
                    PlaylistName = groupChat.PlaylistName ?? "Unnamed Playlist",
                    TracksShared = userTracksInChat.Count,
                    UpvotesGiven = upvotesGiven,
                    DownvotesGiven = downvotesGiven,
                    UpvotesReceived = upvotesReceived,
                    DownvotesReceived = downvotesReceived
                });
            }
        }

        return playlistStats.OrderByDescending(p => p.TracksShared).ToList();
    }

    private async Task<(List<VoterInfo> upvoters, List<VoterInfo> downvoters)> GetVotersInfoAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        // Get all tracks shared by the user.
        var filter = $"SharedByTelegramUserId eq {telegramUserId}L";
        var userTracks = await _trackRecordRepository.QueryAsync(filter, cancellationToken);

        var upvoterCounts = new Dictionary<long, VoterInfo>();
        var downvoterCounts = new Dictionary<long, VoterInfo>();

        foreach (var track in userTracks.Where(t => !t.IsDeleted))
        {
            var votes = await _voteRepository.GetByTrackRecordAsync(track.TrackRecordId, cancellationToken);

            foreach (var vote in votes)
            {
                if (vote.VoteType == "Upvote")
                {
                    if (!upvoterCounts.ContainsKey(vote.TelegramUserId))
                    {
                        upvoterCounts[vote.TelegramUserId] = new VoterInfo
                        {
                            TelegramUserId = vote.TelegramUserId,
                            Username = vote.VoterUsername,
                            AvatarUrl = vote.VoterAvatarUrl,
                            VoteCount = 0
                        };
                    }
                    upvoterCounts[vote.TelegramUserId].VoteCount++;
                }
                else if (vote.VoteType == "Downvote")
                {
                    if (!downvoterCounts.ContainsKey(vote.TelegramUserId))
                    {
                        downvoterCounts[vote.TelegramUserId] = new VoterInfo
                        {
                            TelegramUserId = vote.TelegramUserId,
                            Username = vote.VoterUsername,
                            AvatarUrl = vote.VoterAvatarUrl,
                            VoteCount = 0
                        };
                    }
                    downvoterCounts[vote.TelegramUserId].VoteCount++;
                }
            }
        }

        return (
            upvoterCounts.Values.OrderByDescending(v => v.VoteCount).ToList(),
            downvoterCounts.Values.OrderByDescending(v => v.VoteCount).ToList()
        );
    }
}
