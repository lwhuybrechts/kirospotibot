using KiroSpotiBot.ApiModels;

namespace KiroSpotiBot.Web.Services;

/// <summary>
/// Client interface for user API endpoints.
/// </summary>
public interface IUserApiClient
{
    Task<IEnumerable<UserSummaryDto>> GetUsersAsync();
    Task<UserDetailsDto?> GetUserAsync(long userId);
}
