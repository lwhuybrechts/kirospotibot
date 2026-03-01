using KiroSpotiBot.ApiModels;
using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Functions.Mappers;

/// <summary>
/// Maps playlist-related entities to DTOs.
/// </summary>
public static class PlaylistMapper
{
    /// <summary>
    /// Maps a GroupChatEntity to a PlaylistDto.
    /// </summary>
    public static PlaylistDto ToDto(GroupChatEntity entity)
    {
        return new PlaylistDto
        {
            ChatId = entity.TelegramChatId,
            PlaylistId = entity.PlaylistId ?? string.Empty,
            PlaylistName = entity.PlaylistName ?? string.Empty,
            DownvoteThreshold = entity.DownvoteThreshold,
            AdministratorTelegramUserId = entity.AdministratorTelegramUserId,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Maps a collection of GroupChatEntity to PlaylistDto.
    /// </summary>
    public static IEnumerable<PlaylistDto> ToDto(IEnumerable<GroupChatEntity> entities)
    {
        return entities.Select(ToDto);
    }
}
