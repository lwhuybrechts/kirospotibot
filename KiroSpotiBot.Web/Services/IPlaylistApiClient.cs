using KiroSpotiBot.ApiModels;

namespace KiroSpotiBot.Web.Services;

/// <summary>
/// Client interface for playlist API endpoints.
/// </summary>
public interface IPlaylistApiClient
{
    Task<IEnumerable<PlaylistDto>> GetPlaylistsAsync();
    Task<PlaylistDto?> GetPlaylistAsync(long chatId);
    Task<IEnumerable<TrackDto>> GetPlaylistTracksAsync(long chatId, int skip = 0, int take = 10000);
    Task<IEnumerable<ContributorDto>> GetPlaylistContributorsAsync(long chatId);
    Task<IEnumerable<GenreDto>> GetPlaylistGenresAsync(long chatId);
}
