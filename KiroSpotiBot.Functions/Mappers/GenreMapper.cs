using KiroSpotiBot.ApiModels;
using KiroSpotiBot.Core.Models;

namespace KiroSpotiBot.Functions.Mappers;

/// <summary>
/// Maps genre-related models to DTOs.
/// </summary>
public static class GenreMapper
{
    /// <summary>
    /// Maps a GenreInfo to a GenreDto.
    /// </summary>
    public static GenreDto ToDto(GenreInfo genreInfo)
    {
        return new GenreDto
        {
            GenreName = genreInfo.GenreName,
            TrackCount = genreInfo.TrackCount
        };
    }

    /// <summary>
    /// Maps a collection of GenreInfo to GenreDto.
    /// </summary>
    public static IEnumerable<GenreDto> ToDto(IEnumerable<GenreInfo> genreInfos)
    {
        return genreInfos.Select(ToDto);
    }
}
