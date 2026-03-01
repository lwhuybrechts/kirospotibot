using KiroSpotiBot.ApiModels;
using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Functions.Mappers;

/// <summary>
/// Maps track-related entities to DTOs.
/// </summary>
public static class TrackMapper
{
    /// <summary>
    /// Maps a TrackRecordEntity to a TrackDto with genres and votes.
    /// </summary>
    public static TrackDto ToDto(
        TrackRecordEntity entity,
        List<string> genres,
        List<VoteDto> votes)
    {
        return new TrackDto
        {
            TrackRecordId = entity.TrackRecordId,
            TrackSpotifyId = entity.TrackSpotifyId,
            TrackName = entity.TrackName,
            ArtistName = entity.ArtistName,
            AlbumName = entity.AlbumName,
            AlbumImageUrl = entity.AlbumImageUrl,
            SharedByTelegramUserId = entity.SharedByTelegramUserId,
            SharedByUsername = entity.SharedByUsername,
            SharedByAvatarUrl = entity.SharedByAvatarUrl,
            SharedAt = entity.SharedAt,
            UpvoteCount = entity.UpvoteCount,
            DownvoteCount = entity.DownvoteCount,
            Genres = genres,
            Votes = votes
        };
    }
}
