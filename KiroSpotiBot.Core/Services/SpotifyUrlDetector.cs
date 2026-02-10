using System.Text.RegularExpressions;
using KiroSpotiBot.Core.Interfaces;

namespace KiroSpotiBot.Core.Services;

/// <summary>
/// Service for detecting and extracting Spotify track URLs from text messages.
/// Supports both HTTP URLs (open.spotify.com and play.spotify.com) and Spotify URI format.
/// </summary>
public class SpotifyUrlDetector : ISpotifyUrlDetector
{
    // Regex pattern for Spotify track URLs.
    // Matches:
    // - https://open.spotify.com/track/{trackId}
    // - https://play.spotify.com/track/{trackId}
    // - http://open.spotify.com/track/{trackId}
    // - http://play.spotify.com/track/{trackId}
    // - spotify:track:{trackId}
    // Query parameters (e.g., ?si=...) are handled by the pattern.
    private static readonly Regex TrackUrlPattern = 
        new(@"(https?://(open|play)\.spotify\.com/track/|spotify:track:)([\w\d]+)", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Detects all Spotify track URLs in the provided message text.
    /// </summary>
    /// <param name="messageText">The text to search for Spotify URLs.</param>
    /// <returns>A collection of detected Spotify track URLs.</returns>
    public IEnumerable<string> DetectTrackUrls(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return Enumerable.Empty<string>();
        }

        var matches = TrackUrlPattern.Matches(messageText);
        return matches.Select(m => m.Value).ToList();
    }

    /// <summary>
    /// Extracts the track ID from a Spotify URL.
    /// </summary>
    /// <param name="spotifyUrl">The Spotify URL to extract the track ID from.</param>
    /// <returns>The extracted track ID, or null if the URL is invalid.</returns>
    public string? ExtractTrackId(string spotifyUrl)
    {
        if (string.IsNullOrWhiteSpace(spotifyUrl))
        {
            return null;
        }

        var match = TrackUrlPattern.Match(spotifyUrl);
        if (!match.Success)
        {
            return null;
        }

        // The track ID is in the third capture group.
        return match.Groups[3].Value;
    }
}
