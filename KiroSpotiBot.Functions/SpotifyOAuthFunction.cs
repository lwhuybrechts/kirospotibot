using KiroSpotiBot.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Functions;

/// <summary>
/// Azure Function for handling Spotify OAuth authentication flow.
/// Acts as a thin controller that delegates to ISpotifyOAuthHandler.
/// </summary>
public class SpotifyOAuthFunction
{
    private readonly ILogger<SpotifyOAuthFunction> _logger;
    private readonly ISpotifyOAuthHandler _oauthHandler;

    public SpotifyOAuthFunction(
        ILogger<SpotifyOAuthFunction> logger,
        ISpotifyOAuthHandler oauthHandler)
    {
        _logger = logger;
        _oauthHandler = oauthHandler;
    }

    /// <summary>
    /// Initiates the Spotify OAuth flow.
    /// </summary>
    [Function("SpotifyOAuthStart")]
    public async Task<IActionResult> StartAuth(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "auth/spotify/start")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract and validate query parameters.
            if (!long.TryParse(req.Query["telegramUserId"], out var telegramUserId))
            {
                _logger.LogWarning("Invalid or missing telegramUserId parameter.");
                return new BadRequestObjectResult(new { error = "Invalid or missing telegramUserId parameter." });
            }

            if (!long.TryParse(req.Query["chatId"], out var chatId))
            {
                _logger.LogWarning("Invalid or missing chatId parameter.");
                return new BadRequestObjectResult(new { error = "Invalid or missing chatId parameter." });
            }

            // Delegate to handler for business logic.
            var authorizationUrl = await _oauthHandler.StartAuthAsync(telegramUserId, chatId, cancellationToken);

            _logger.LogInformation("Redirecting user {TelegramUserId} to Spotify authorization URL.", telegramUserId);

            // Redirect to Spotify authorization URL.
            return new RedirectResult(authorizationUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Spotify OAuth flow.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Handles the OAuth callback from Spotify.
    /// </summary>
    [Function("SpotifyOAuthCallback")]
    public async Task<IActionResult> HandleCallback(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "auth/spotify/callback")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract query parameters.
            var code = req.Query["code"].ToString();
            var state = req.Query["state"].ToString();
            var error = req.Query["error"].ToString();

            // Delegate to handler for business logic.
            var result = await _oauthHandler.HandleCallbackAsync(code, state, error, cancellationToken);

            // Return appropriate response based on result.
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return new BadRequestObjectResult(new { error = result.ErrorMessage });
            }

            return new ContentResult
            {
                Content = result.HtmlContent,
                ContentType = "text/html",
                StatusCode = result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OAuth callback.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
