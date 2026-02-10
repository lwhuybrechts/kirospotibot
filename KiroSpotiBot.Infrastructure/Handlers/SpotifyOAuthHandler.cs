using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Options;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using Telegram.Bot;

namespace KiroSpotiBot.Infrastructure.Handlers;

/// <summary>
/// Handles Spotify OAuth authentication flow business logic.
/// </summary>
public class SpotifyOAuthHandler : ISpotifyOAuthHandler
{
    private readonly ILogger<SpotifyOAuthHandler> _logger;
    private readonly IOAuthStateRepository _oauthStateRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly SpotifyOptions _spotifyOptions;

    public SpotifyOAuthHandler(
        ILogger<SpotifyOAuthHandler> logger,
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
    /// Initiates the OAuth flow and returns the authorization URL.
    /// </summary>
    public async Task<string> StartAuthAsync(long telegramUserId, long chatId, CancellationToken cancellationToken)
    {
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

        _logger.LogInformation("Generated Spotify authorization URL for user {TelegramUserId}.", telegramUserId);

        return authorizationUrl;
    }

    /// <summary>
    /// Handles the OAuth callback and completes the authentication process.
    /// </summary>
    public async Task<OAuthCallbackResult> HandleCallbackAsync(
        string? code,
        string? state,
        string? error,
        CancellationToken cancellationToken)
    {
        // Check if user cancelled or error occurred.
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth flow cancelled or error occurred: {Error}", error);
            return new OAuthCallbackResult
            {
                IsSuccess = false,
                HtmlContent = "<html><body><h1>Authentication Cancelled</h1><p>You can close this window.</p></body></html>"
            };
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            _logger.LogWarning("Missing code or state parameter in OAuth callback.");
            return new OAuthCallbackResult
            {
                IsSuccess = false,
                ErrorMessage = "Missing code or state parameter."
            };
        }

        // Validate state parameter.
        var oauthState = await _oauthStateRepository.GetByStateAsync(state, cancellationToken);
        if (oauthState == null)
        {
            _logger.LogWarning("Invalid or expired OAuth state: {State}", state);
            return new OAuthCallbackResult
            {
                IsSuccess = false,
                ErrorMessage = "Invalid or expired state parameter."
            };
        }

        // Check if state has expired.
        if (oauthState.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("OAuth state {State} has expired.", state);
            await _oauthStateRepository.DeleteAsync(state, cancellationToken);
            return new OAuthCallbackResult
            {
                IsSuccess = false,
                ErrorMessage = "OAuth state has expired. Please try again."
            };
        }

        try
        {
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
            await SendTelegramConfirmationAsync(oauthState, cancellationToken);

            // Return success page.
            return new OAuthCallbackResult
            {
                IsSuccess = true,
                HtmlContent = "<html><body><h1>Authentication Successful!</h1><p>You have successfully authenticated with Spotify. You can close this window and return to Telegram.</p></body></html>"
            };
        }
        catch (APIException ex)
        {
            _logger.LogError(ex, "Spotify API error during OAuth callback: {Message}", ex.Message);
            return new OAuthCallbackResult
            {
                IsSuccess = false,
                HtmlContent = "<html><body><h1>Authentication Failed</h1><p>An error occurred while authenticating with Spotify. Please try again.</p></body></html>"
            };
        }
    }

    /// <summary>
    /// Sends a confirmation message to the user via Telegram.
    /// </summary>
    private async Task SendTelegramConfirmationAsync(OAuthStateEntity oauthState, CancellationToken cancellationToken)
    {
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
    }
}
