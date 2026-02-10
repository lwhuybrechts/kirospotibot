namespace KiroSpotiBot.Core.Interfaces;

/// <summary>
/// Service for detecting and extracting Spotify track URLs from text messages.
/// </summary>
public interface ISpotifyUrlDetector
{
    /// <summary>
    /// Detects all Spotify track URLs in the provided message text.
    /// </summary>
    /// <param name="messageText">The text to search for Spotify URLs.</param>
    /// <returns>A collection of detected Spotify track URLs.</returns>
    IEnumerable<string> DetectTrackUrls(string messageText);

    /// <summary>
    /// Extracts the track ID from a Spotify URL.
    /// </summary>
    /// <param name="spotifyUrl">The Spotify URL to extract the track ID from.</param>
    /// <returns>The extracted track ID, or null if the URL is invalid.</returns>
    string? ExtractTrackId(string spotifyUrl);
}
