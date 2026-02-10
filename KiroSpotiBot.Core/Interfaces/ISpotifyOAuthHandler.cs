namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Handles Spotify OAuth authentication flow.
/// </summary>
public interface ISpotifyOAuthHandler
{
    /// <summary>
    /// Initiates the OAuth flow and returns the authorization URL.
    /// </summary>
    Task<string> StartAuthAsync(long telegramUserId, long chatId, CancellationToken cancellationToken);

    /// <summary>
    /// Handles the OAuth callback and completes the authentication process.
    /// </summary>
    Task<OAuthCallbackResult> HandleCallbackAsync(string? code, string? state, string? error, CancellationToken cancellationToken);
}

/// <summary>
/// Result of OAuth callback processing.
/// </summary>
public class OAuthCallbackResult
{
    /// <summary>
    /// Indicates whether the OAuth flow was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// HTML content to display to the user.
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Error message if the flow failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
