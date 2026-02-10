using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Options;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using Telegram.Bot;

namespace KiroSpotiBot.Functions;

/// <summary>
/// Azure Function for handling Spotify OAuth authentication flow.
/// </summary>
public class SpotifyOAuthFunction
{
    private readonly ILogger<SpotifyOAuthFunction> _logger;
    private readonly IOAuthStateRepository _oauthStateRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly SpotifyOptions _spotifyOptions;

    public SpotifyOAuthFunction(
        ILogger<SpotifyOAuthFunction> logger,
        IOAuthStateRepository oauthStateRepository,
        IUserRepository userRepository,
        ITelegramBotClient telegramBotClient,
        IOptions<SpotifyOptions> spotifyOptions)
    {
        _logger = logger;
        _oauthStateRepository = oauthStateRepository;
        _userRepository = userRepository;
        _telegramBotClient = telegramBotClient;
        _spotifyOptions = spotifyOptions.Value;
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
            // Extract query parameters.
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

            // Generate OAuth state parameter for CSRF protection.
            var state = Guid.NewGuid().ToString();
            var oauthState = new OAuthStateEntity(state, telegramUserId, chatId);
            await _oauthStateRepository.CreateAsync(oauthState, cancellationToken);

            _logger.LogInformation("Created OAuth state {State} for user {TelegramUserId}.", state, telegramUserId);

            // Build Spotify authorization URL with required scopes.
            var loginRequest = new LoginRequest(
                new Uri(_spotifyOptions.RedirectUri),
                _spotifyOptions.ClientId,
                LoginRequest.ResponseType.Code)
            {
                Scope = new[]
                {
                    Scopes.PlaylistModifyPublic,
                    Scopes.PlaylistModifyPrivate,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadPlaybackState
                },
                State = state
            };

            var authorizationUrl = loginRequest.ToUri().ToString();

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

            // Check if user cancelled or error occurred.
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("OAuth flow cancelled or error occurred: {Error}", error);
                return new ContentResult
                {
                    Content = "<html><body><h1>Authentication Cancelled</h1><p>You can close this window.</p></body></html>",
                    ContentType = "text/html",
                    StatusCode = StatusCodes.Status200OK
                };
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                _logger.LogWarning("Missing code or state parameter in OAuth callback.");
                return new BadRequestObjectResult(new { error = "Missing code or state parameter." });
            }

            // Validate state parameter.
            var oauthState = await _oauthStateRepository.GetByStateAsync(state, cancellationToken);
            if (oauthState == null)
            {
                _logger.LogWarning("Invalid or expired OAuth state: {State}", state);
                return new BadRequestObjectResult(new { error = "Invalid or expired state parameter." });
            }

            // Check if state has expired.
            if (oauthState.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("OAuth state {State} has expired.", state);
                await _oauthStateRepository.DeleteAsync(state, cancellationToken);
                return new BadRequestObjectResult(new { error = "OAuth state has expired. Please try again." });
            }

            // Exchange authorization code for tokens.
            var config = SpotifyClientConfig.CreateDefault();
            var tokenRequest = new AuthorizationCodeTokenRequest(
                _spotifyOptions.ClientId,
                _spotifyOptions.ClientSecret,
                code,
                new Uri(_spotifyOptions.RedirectUri));

            var tokenResponse = await new OAuthClient(config).RequestToken(tokenRequest, cancellationToken);

            _logger.LogInformation("Successfully exchanged authorization code for tokens for user {TelegramUserId}.", oauthState.TelegramUserId);

            // Store encrypted credentials in database.
            await _userRepository.UpdateSpotifyCredentialsAsync(
                oauthState.TelegramUserId,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken,
                tokenResponse.ExpiresIn,
                tokenResponse.Scope,
                cancellationToken);

            _logger.LogInformation("Stored Spotify credentials for user {TelegramUserId}.", oauthState.TelegramUserId);

            // Delete OAuth state.
            await _oauthStateRepository.DeleteAsync(state, cancellationToken);

            // Send confirmation via Telegram private message.
            try
            {
                await _telegramBotClient.SendMessage(
                    chatId: oauthState.TelegramChatId,
                    text: "✅ Successfully authenticated with Spotify! You can now use all bot features.",
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Sent authentication confirmation to user {TelegramUserId}.", oauthState.TelegramUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram confirmation message to user {TelegramUserId}.", oauthState.TelegramUserId);
                // Don't fail the OAuth flow if Telegram message fails.
            }

            // Return success page.
            return new ContentResult
            {
                Content = "<html><body><h1>Authentication Successful!</h1><p>You have successfully authenticated with Spotify. You can close this window and return to Telegram.</p></body></html>",
                ContentType = "text/html",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error during OAuth callback: {Message}", ex.Message);
            return new ContentResult
            {
                Content = "<html><body><h1>Authentication Failed</h1><p>An error occurred while authenticating with Spotify. Please try again.</p></body></html>",
                ContentType = "text/html",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OAuth callback.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
