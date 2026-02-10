using System.ComponentModel.DataAnnotations;

namespace KiroSpotiBot.Infrastructure.Options;

/// <summary>
/// Configuration options for Spotify API integration.
/// </summary>
public class SpotifyOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Spotify";

    /// <summary>
    /// The Spotify application client ID.
    /// </summary>
    [Required(ErrorMessage = "Spotify:ClientId is required.")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Spotify application client secret.
    /// </summary>
    [Required(ErrorMessage = "Spotify:ClientSecret is required.")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth redirect URI for authentication callbacks.
    /// </summary>
    [Required(ErrorMessage = "Spotify:RedirectUri is required.")]
    public string RedirectUri { get; set; } = string.Empty;
}
