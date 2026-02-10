using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Core.Services;

namespace KiroSpotiBot.Tests;

/// <summary>
/// Unit tests for SpotifyUrlDetector service.
/// Tests URL detection and track ID extraction functionality.
/// </summary>
public class SpotifyUrlDetectorTests
{
    private readonly ISpotifyUrlDetector _detector;

    public SpotifyUrlDetectorTests()
    {
        _detector = new SpotifyUrlDetector();
    }

    #region DetectTrackUrls Tests

    [Fact]
    public void DetectTrackUrls_WithOpenSpotifyHttpsUrl_ReturnsUrl()
    {
        // Arrange.
        var message = "Check out this track: https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Single(result);
        Assert.Contains("https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp", result);
    }

    [Fact]
    public void DetectTrackUrls_WithPlaySpotifyUrl_ReturnsUrl()
    {
        // Arrange.
        var message = "Listen to https://play.spotify.com/track/7ouMYWpwJ422jRcDASZB7P";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Single(result);
        Assert.Contains("https://play.spotify.com/track/7ouMYWpwJ422jRcDASZB7P", result);
    }

    [Fact]
    public void DetectTrackUrls_WithSpotifyUri_ReturnsUri()
    {
        // Arrange.
        var message = "spotify:track:4cOdK2wGLETKBW3PvgPWqT is awesome!";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Single(result);
        Assert.Contains("spotify:track:4cOdK2wGLETKBW3PvgPWqT", result);
    }

    [Fact]
    public void DetectTrackUrls_WithQueryParameters_ReturnsUrlWithoutQueryParams()
    {
        // Arrange.
        var message = "https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp?si=abc123def456";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Single(result);
        // The regex captures the URL without query parameters.
        Assert.Contains("https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp", result.First());
    }

    [Fact]
    public void DetectTrackUrls_WithMultipleUrls_ReturnsAllUrls()
    {
        // Arrange.
        var message = "Check these out: https://open.spotify.com/track/abc123 and spotify:track:def456";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void DetectTrackUrls_WithNoUrls_ReturnsEmpty()
    {
        // Arrange.
        var message = "This is just a regular message with no Spotify links";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Empty(result);
    }

    [Fact]
    public void DetectTrackUrls_WithEmptyString_ReturnsEmpty()
    {
        // Arrange.
        var message = "";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Empty(result);
    }

    [Fact]
    public void DetectTrackUrls_WithNull_ReturnsEmpty()
    {
        // Arrange.
        string? message = null;

        // Act.
        var result = _detector.DetectTrackUrls(message!);

        // Assert.
        Assert.Empty(result);
    }

    [Fact]
    public void DetectTrackUrls_WithWhitespace_ReturnsEmpty()
    {
        // Arrange.
        var message = "   \t\n  ";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Empty(result);
    }

    [Fact]
    public void DetectTrackUrls_WithHttpUrl_ReturnsUrl()
    {
        // Arrange.
        var message = "http://open.spotify.com/track/abc123xyz";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Single(result);
        Assert.Contains("http://open.spotify.com/track/abc123xyz", result);
    }

    [Fact]
    public void DetectTrackUrls_WithAlbumUrl_ReturnsEmpty()
    {
        // Arrange - album URL should not be detected.
        var message = "https://open.spotify.com/album/abc123";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Empty(result);
    }

    [Fact]
    public void DetectTrackUrls_WithPlaylistUrl_ReturnsEmpty()
    {
        // Arrange - playlist URL should not be detected.
        var message = "https://open.spotify.com/playlist/abc123";

        // Act.
        var result = _detector.DetectTrackUrls(message);

        // Assert.
        Assert.Empty(result);
    }

    #endregion

    #region ExtractTrackId Tests

    [Fact]
    public void ExtractTrackId_WithOpenSpotifyUrl_ReturnsTrackId()
    {
        // Arrange.
        var url = "https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp";

        // Act.
        var result = _detector.ExtractTrackId(url);

        // Assert.
        Assert.Equal("3n3Ppam7vgaVa1iaRUc9Lp", result);
    }

    [Fact]
    public void ExtractTrackId_WithPlaySpotifyUrl_ReturnsTrackId()
    {
        // Arrange.
        var url = "https://play.spotify.com/track/7ouMYWpwJ422jRcDASZB7P";

        // Act.
        var result = _detector.ExtractTrackId(url);

        // Assert.
        Assert.Equal("7ouMYWpwJ422jRcDASZB7P", result);
    }

    [Fact]
    public void ExtractTrackId_WithSpotifyUri_ReturnsTrackId()
    {
        // Arrange.
        var uri = "spotify:track:4cOdK2wGLETKBW3PvgPWqT";

        // Act.
        var result = _detector.ExtractTrackId(uri);

        // Assert.
        Assert.Equal("4cOdK2wGLETKBW3PvgPWqT", result);
    }

    [Fact]
    public void ExtractTrackId_WithQueryParameters_ReturnsTrackId()
    {
        // Arrange.
        var url = "https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp?si=abc123def456";

        // Act.
        var result = _detector.ExtractTrackId(url);

        // Assert.
        Assert.Equal("3n3Ppam7vgaVa1iaRUc9Lp", result);
    }

    [Fact]
    public void ExtractTrackId_WithInvalidUrl_ReturnsNull()
    {
        // Arrange.
        var url = "https://example.com/not-a-spotify-url";

        // Act.
        var result = _detector.ExtractTrackId(url);

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public void ExtractTrackId_WithEmptyString_ReturnsNull()
    {
        // Arrange.
        var url = "";

        // Act.
        var result = _detector.ExtractTrackId(url);

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public void ExtractTrackId_WithNull_ReturnsNull()
    {
        // Arrange.
        string? url = null;

        // Act.
        var result = _detector.ExtractTrackId(url!);

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public void ExtractTrackId_WithAlbumUrl_ReturnsNull()
    {
        // Arrange.
        var url = "https://open.spotify.com/album/abc123";

        // Act.
        var result = _detector.ExtractTrackId(url);

        // Assert.
        Assert.Null(result);
    }

    [Fact]
    public void ExtractTrackId_WithHttpUrl_ReturnsTrackId()
    {
        // Arrange.
        var url = "http://open.spotify.com/track/abc123xyz";

        // Act.
        var result = _detector.ExtractTrackId(url);

        // Assert.
        Assert.Equal("abc123xyz", result);
    }

    [Fact]
    public void ExtractTrackId_WithMixedCaseUrl_ReturnsTrackId()
    {
        // Arrange.
        var url = "HTTPS://OPEN.SPOTIFY.COM/TRACK/abc123XYZ";

        // Act.
        var result = _detector.ExtractTrackId(url);

        // Assert.
        Assert.Equal("abc123XYZ", result);
    }

    #endregion
}
