using KiroSpotiBot.ApiModels;
using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Functions.Mappers;

/// <summary>
/// Maps vote-related entities to DTOs.
/// </summary>
public static class VoteMapper
{
    /// <summary>
    /// Maps a VoteEntity to a VoteDto.
    /// </summary>
    public static VoteDto ToDto(VoteEntity entity)
    {
        return new VoteDto
        {
            TelegramUserId = entity.TelegramUserId,
            VoteType = entity.VoteType,
            VoterUsername = entity.VoterUsername,
            VoterAvatarUrl = entity.VoterAvatarUrl,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Maps a collection of VoteEntity to VoteDto.
    /// </summary>
    public static List<VoteDto> ToDto(IEnumerable<VoteEntity> entities)
    {
        return entities.Select(ToDto).ToList();
    }
}
