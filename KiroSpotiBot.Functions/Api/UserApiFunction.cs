using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.ApiModels;
using KiroSpotiBot.Functions.Mappers;

namespace KiroSpotiBot.Functions.Api;

/// <summary>
/// API endpoints for user data access from the Blazor WebAssembly frontend.
/// </summary>
public class UserApiFunction
{
    private readonly ILogger<UserApiFunction> _logger;
    private readonly IUserStatisticsService _userStatisticsService;

    public UserApiFunction(
        ILogger<UserApiFunction> logger,
        IUserStatisticsService userStatisticsService)
    {
        _logger = logger;
        _userStatisticsService = userStatisticsService;
    }

    /// <summary>
    /// Gets all users with their statistics.
    /// </summary>
    [Function("GetUsers")]
    public async Task<IActionResult> GetUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/users")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all users with statistics.");

            var users = await _userStatisticsService.GetAllUsersWithStatisticsAsync(cancellationToken);

            var result = UserMapper.ToDto(users);

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets a specific user with detailed statistics.
    /// </summary>
    [Function("GetUser")]
    public async Task<IActionResult> GetUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/users/{userId:long}")] HttpRequest req,
        long userId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting user details for user ID: {UserId}", userId);

            var userDetails = await _userStatisticsService.GetUserDetailsAsync(userId, cancellationToken);

            if (userDetails == null)
            {
                return new NotFoundResult();
            }

            var result = UserMapper.ToDto(userDetails);

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details for user ID: {UserId}", userId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
