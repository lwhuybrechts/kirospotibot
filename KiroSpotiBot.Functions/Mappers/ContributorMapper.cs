using KiroSpotiBot.ApiModels;
using KiroSpotiBot.Core.Models;

namespace KiroSpotiBot.Functions.Mappers;

/// <summary>
/// Maps contributor-related models to DTOs.
/// </summary>
public static class ContributorMapper
{
    /// <summary>
    /// Maps a Contributor to a ContributorDto.
    /// </summary>
    public static ContributorDto ToDto(Contributor contributor)
    {
        return new ContributorDto
        {
            TelegramUserId = contributor.TelegramUserId,
            Username = contributor.Username,
            AvatarUrl = contributor.AvatarUrl,
            TrackCount = contributor.TrackCount
        };
    }

    /// <summary>
    /// Maps a collection of Contributor to ContributorDto.
    /// </summary>
    public static IEnumerable<ContributorDto> ToDto(IEnumerable<Contributor> contributors)
    {
        return contributors.Select(ToDto);
    }
}
