using System.Net.Http.Json;
using KiroSpotiBot.ApiModels;

namespace KiroSpotiBot.Web.Services;

/// <summary>
/// HTTP client for user API endpoints.
/// </summary>
public class UserApiClient : IUserApiClient
{
    private readonly HttpClient _httpClient;

    public UserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<UserSummaryDto>> GetUsersAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<IEnumerable<UserSummaryDto>>("api/users");
        return result ?? Enumerable.Empty<UserSummaryDto>();
    }

    public async Task<UserDetailsDto?> GetUserAsync(long userId)
    {
        return await _httpClient.GetFromJsonAsync<UserDetailsDto>($"api/users/{userId}");
    }
}
