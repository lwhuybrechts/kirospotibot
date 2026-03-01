using KiroSpotiBot.ApiModels;
using KiroSpotiBot.Core.Interfaces;

namespace KiroSpotiBot.Functions.Mappers;

/// <summary>
/// Maps user-related models to DTOs.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Maps a UserSummary to a UserSummaryDto.
    /// </summary>
    public static UserSummaryDto ToDto(UserSummary user)
    {
        return new UserSummaryDto
        {
            TelegramUserId = user.TelegramUserId,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            TotalTracksShared = user.TotalTracksShared,
            TotalUpvotesGiven = user.TotalUpvotesGiven,
            TotalDownvotesGiven = user.TotalDownvotesGiven,
            TotalUpvotesReceived = user.TotalUpvotesReceived,
            TotalDownvotesReceived = user.TotalDownvotesReceived
        };
    }

    /// <summary>
    /// Maps a collection of UserSummary to UserSummaryDto.
    /// </summary>
    public static IEnumerable<UserSummaryDto> ToDto(IEnumerable<UserSummary> users)
    {
        return users.Select(ToDto);
    }

    /// <summary>
    /// Maps a UserDetails to a UserDetailsDto.
    /// </summary>
    public static UserDetailsDto ToDto(UserDetails userDetails)
    {
        return new UserDetailsDto
        {
            TelegramUserId = userDetails.TelegramUserId,
            Username = userDetails.Username,
            AvatarUrl = userDetails.AvatarUrl,
            TotalTracksShared = userDetails.TotalTracksShared,
            TotalUpvotesGiven = userDetails.TotalUpvotesGiven,
            TotalDownvotesGiven = userDetails.TotalDownvotesGiven,
            TotalUpvotesReceived = userDetails.TotalUpvotesReceived,
            TotalDownvotesReceived = userDetails.TotalDownvotesReceived,
            PlaylistStatistics = userDetails.PlaylistStatistics.Select(ToDto).ToList(),
            SharedTracks = userDetails.SharedTracks.Select(ToDto).ToList(),
            VotersOnUserTracks = userDetails.VotersOnUserTracks.Select(ToDto).ToList()
        };
    }

    /// <summary>
    /// Maps a PlaylistStatistics to a PlaylistStatisticsDto.
    /// </summary>
    private static PlaylistStatisticsDto ToDto(PlaylistStatistics ps)
    {
        return new PlaylistStatisticsDto
        {
            ChatId = ps.ChatId,
            PlaylistName = ps.PlaylistName,
            TracksShared = ps.TracksShared,
            UpvotesGiven = ps.UpvotesGiven,
            DownvotesGiven = ps.DownvotesGiven,
            UpvotesReceived = ps.UpvotesReceived,
            DownvotesReceived = ps.DownvotesReceived
        };
    }

    /// <summary>
    /// Maps a SharedTrack to a SharedTrackDto.
    /// </summary>
    private static SharedTrackDto ToDto(SharedTrack st)
    {
        return new SharedTrackDto
        {
            TrackRecordId = st.TrackRecordId,
            TrackSpotifyId = st.TrackSpotifyId,
            TrackName = st.TrackName,
            ArtistName = st.ArtistName,
            AlbumName = st.AlbumName,
            AlbumImageUrl = st.AlbumImageUrl,
            ChatId = st.ChatId,
            PlaylistName = st.PlaylistName,
            SharedAt = st.SharedAt,
            UpvoteCount = st.UpvoteCount,
            DownvoteCount = st.DownvoteCount
        };
    }

    /// <summary>
    /// Maps a VoterInfo to a VoterDto.
    /// </summary>
    private static VoterDto ToDto(VoterInfo v)
    {
        return new VoterDto
        {
            TelegramUserId = v.TelegramUserId,
            Username = v.Username,
            AvatarUrl = v.AvatarUrl,
            UpvoteCount = v.UpvoteCount,
            DownvoteCount = v.DownvoteCount
        };
    }
}
