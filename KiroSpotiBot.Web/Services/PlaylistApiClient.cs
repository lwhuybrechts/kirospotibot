using System.Net.Http.Json;
using KiroSpotiBot.ApiModels;

namespace KiroSpotiBot.Web.Services;

/// <summary>
/// HTTP client for playlist API endpoints.
/// </summary>
public class PlaylistApiClient : IPlaylistApiClient
{
    private readonly HttpClient _httpClient;

    public PlaylistApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<PlaylistDto>> GetPlaylistsAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<IEnumerable<PlaylistDto>>("api/playlists");
        return result ?? Enumerable.Empty<PlaylistDto>();
    }

    public async Task<PlaylistDto?> GetPlaylistAsync(long chatId)
    {
        return await _httpClient.GetFromJsonAsync<PlaylistDto>($"api/playlists/{chatId}");
    }

    public async Task<IEnumerable<TrackDto>> GetPlaylistTracksAsync(long chatId, int skip = 0, int take = 10000)
    {
        var result = await _httpClient.GetFromJsonAsync<IEnumerable<TrackDto>>(
            $"api/playlists/{chatId}/tracks?skip={skip}&take={take}");
        return result ?? Enumerable.Empty<TrackDto>();
    }

    public async Task<IEnumerable<ContributorDto>> GetPlaylistContributorsAsync(long chatId)
    {
        var result = await _httpClient.GetFromJsonAsync<IEnumerable<ContributorDto>>(
            $"api/playlists/{chatId}/contributors");
        return result ?? Enumerable.Empty<ContributorDto>();
    }

    public async Task<IEnumerable<GenreDto>> GetPlaylistGenresAsync(long chatId)
    {
        var result = await _httpClient.GetFromJsonAsync<IEnumerable<GenreDto>>(
            $"api/playlists/{chatId}/genres");
        return result ?? Enumerable.Empty<GenreDto>();
    }
}
