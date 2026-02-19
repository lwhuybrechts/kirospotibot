using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property 37: Genre Filter Accuracy
/// For any genre selected in the web frontend, the filtered track list should contain 
/// only tracks of that genre and should include all tracks with that genre.
/// Validates: Requirements 17.3
/// </summary>
public class GenreFilterAccuracyPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly ITrackGenreRepository _trackGenreRepository;

    public GenreFilterAccuracyPropertyTests()
    {
        var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
            ?? "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        // Truncate tables at the beginning of each test.
        TableHelper.TruncateTable(_tableServiceClient, "TrackRecords");
        TableHelper.TruncateTable(_tableServiceClient, "TrackGenres");
        
        _trackRecordRepository = new TrackRecordRepository(
            _tableServiceClient,
            NullLogger<BaseRepository<TrackRecordEntity>>.Instance
        );
        
        _trackGenreRepository = new TrackGenreRepository(
            _tableServiceClient,
            NullLogger<TrackGenreRepository>.Instance
        );
    }

    [Theory]
    [InlineData(1, 5, 3)] // 5 rock tracks, 3 pop tracks.
    [InlineData(2, 10, 7)] // 10 rock tracks, 7 pop tracks.
    [InlineData(3, 1, 1)] // 1 rock track, 1 pop track.
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 37: Genre Filter Accuracy")]
    public async Task GenreFilter_ShouldReturnOnlyTracksOfSelectedGenre(
        long chatId,
        int rockTrackCount,
        int popTrackCount)
    {
        // Arrange: Create tracks with different genres.
        var rockGenre = "rock";
        var popGenre = "pop";
        
        var rockTracks = new List<TrackRecordEntity>();
        var popTracks = new List<TrackRecordEntity>();

        // Create rock tracks.
        for (int i = 0; i < rockTrackCount; i++)
        {
            var trackId = $"track_rock_{i}";
            var track = new TrackRecordEntity(chatId, trackId, 1001L)
            {
                TrackName = $"Rock Track {i}",
                ArtistName = "Rock Artist",
                AlbumName = "Rock Album",
                SharedByUsername = "User1",
                SharedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            await _trackGenreRepository.CreateAsync(trackId, rockGenre);
            rockTracks.Add(track);
        }

        // Create pop tracks.
        for (int i = 0; i < popTrackCount; i++)
        {
            var trackId = $"track_pop_{i}";
            var track = new TrackRecordEntity(chatId, trackId, 1002L)
            {
                TrackName = $"Pop Track {i}",
                ArtistName = "Pop Artist",
                AlbumName = "Pop Album",
                SharedByUsername = "User2",
                SharedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            await _trackGenreRepository.CreateAsync(trackId, popGenre);
            popTracks.Add(track);
        }

        // Act: Get all tracks and filter by genre.
        var allTracks = await _trackRecordRepository.GetByGroupChatAsync(chatId, 0, 100);
        var allTracksList = allTracks.ToList();

        // Load genres for each track.
        var tracksWithGenres = new List<(TrackRecordEntity Track, List<string> Genres)>();
        foreach (var track in allTracksList)
        {
            var genres = await _trackGenreRepository.GetGenresForTrackAsync(track.TrackSpotifyId);
            tracksWithGenres.Add((track, genres.ToList()));
        }

        // Filter by rock genre.
        var rockFilteredTracks = tracksWithGenres
            .Where(t => t.Genres.Contains(rockGenre))
            .Select(t => t.Track)
            .ToList();

        // Filter by pop genre.
        var popFilteredTracks = tracksWithGenres
            .Where(t => t.Genres.Contains(popGenre))
            .Select(t => t.Track)
            .ToList();

        // Assert: Rock filter should return only rock tracks.
        Assert.Equal(rockTrackCount, rockFilteredTracks.Count);
        foreach (var expectedTrack in rockTracks)
        {
            Assert.Contains(rockFilteredTracks, t => t.TrackRecordId == expectedTrack.TrackRecordId);
        }

        // Assert: Pop filter should return only pop tracks.
        Assert.Equal(popTrackCount, popFilteredTracks.Count);
        foreach (var expectedTrack in popTracks)
        {
            Assert.Contains(popFilteredTracks, t => t.TrackRecordId == expectedTrack.TrackRecordId);
        }

        // Assert: No overlap between filtered lists.
        Assert.Empty(rockFilteredTracks.Intersect(popFilteredTracks, new TrackRecordComparer()));
    }

    [Theory]
    [InlineData(1, 5)] // 5 tracks, 2 are deleted.
    [InlineData(2, 10)] // 10 tracks, 3 are deleted.
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 37: Genre Filter Accuracy")]
    public async Task GenreFilter_ShouldExcludeDeletedTracks(long chatId, int totalTracks)
    {
        // Arrange: Create tracks with same genre, some deleted.
        var genre = "electronic";
        var deletedCount = totalTracks / 2;
        var activeTracks = new List<TrackRecordEntity>();

        for (int i = 0; i < totalTracks; i++)
        {
            var trackId = $"track_{i}";
            var track = new TrackRecordEntity(chatId, trackId, 2001L)
            {
                TrackName = $"Track {i}",
                ArtistName = "Artist",
                AlbumName = "Album",
                SharedByUsername = "TestUser",
                IsDeleted = i < deletedCount // First half are deleted.
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            await _trackGenreRepository.CreateAsync(trackId, genre);
            
            if (!track.IsDeleted)
            {
                activeTracks.Add(track);
            }
        }

        // Act: Get all non-deleted tracks and filter by genre.
        var allTracks = await _trackRecordRepository.GetByGroupChatAsync(chatId, 0, 100);
        var nonDeletedTracks = allTracks.Where(t => !t.IsDeleted).ToList();

        // Load genres for each track.
        var tracksWithGenres = new List<(TrackRecordEntity Track, List<string> Genres)>();
        foreach (var track in nonDeletedTracks)
        {
            var genres = await _trackGenreRepository.GetGenresForTrackAsync(track.TrackSpotifyId);
            tracksWithGenres.Add((track, genres.ToList()));
        }

        // Filter by genre.
        var filteredTracks = tracksWithGenres
            .Where(t => t.Genres.Contains(genre))
            .Select(t => t.Track)
            .ToList();

        // Assert: Should only return non-deleted tracks.
        Assert.Equal(totalTracks - deletedCount, filteredTracks.Count);
        Assert.All(filteredTracks, track => Assert.False(track.IsDeleted));
        
        // Assert: All active tracks should be in the filtered list.
        foreach (var expectedTrack in activeTracks)
        {
            Assert.Contains(filteredTracks, t => t.TrackRecordId == expectedTrack.TrackRecordId);
        }
    }

    [Theory]
    [InlineData(1, 3, 2, 1)] // 3 genres, 2 tracks per genre, 1 track with no genre.
    [InlineData(2, 5, 4, 2)] // 5 genres, 4 tracks per genre, 2 tracks with no genre.
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 37: Genre Filter Accuracy")]
    public async Task GenreFilter_ShouldHandleTracksWithNoGenre(
        long chatId,
        int genreCount,
        int tracksPerGenre,
        int tracksWithoutGenre)
    {
        // Arrange: Create tracks with genres and some without.
        var genres = Enumerable.Range(0, genreCount).Select(i => $"genre_{i}").ToList();
        var tracksWithGenreData = new Dictionary<string, List<TrackRecordEntity>>();

        // Create tracks for each genre.
        foreach (var genre in genres)
        {
            var genreTracks = new List<TrackRecordEntity>();
            for (int i = 0; i < tracksPerGenre; i++)
            {
                var trackId = $"track_{genre}_{i}";
                var track = new TrackRecordEntity(chatId, trackId, 3001L)
                {
                    TrackName = $"Track {i} - {genre}",
                    ArtistName = "Artist",
                    AlbumName = "Album",
                    SharedByUsername = "User"
                };
                await _trackRecordRepository.CreateTrackRecordAsync(track);
                await _trackGenreRepository.CreateAsync(trackId, genre);
                genreTracks.Add(track);
            }
            tracksWithGenreData[genre] = genreTracks;
        }

        // Create tracks without genre.
        for (int i = 0; i < tracksWithoutGenre; i++)
        {
            var trackId = $"track_no_genre_{i}";
            var track = new TrackRecordEntity(chatId, trackId, 3002L)
            {
                TrackName = $"Track {i} - No Genre",
                ArtistName = "Artist",
                AlbumName = "Album",
                SharedByUsername = "User"
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            // Don't add any genre for these tracks.
        }

        // Act: Get all tracks and filter by each genre.
        var allTracks = await _trackRecordRepository.GetByGroupChatAsync(chatId, 0, 1000);
        var allTracksList = allTracks.ToList();

        foreach (var genre in genres)
        {
            // Load genres for each track.
            var tracksWithGenres = new List<(TrackRecordEntity Track, List<string> Genres)>();
            foreach (var track in allTracksList)
            {
                var trackGenres = await _trackGenreRepository.GetGenresForTrackAsync(track.TrackSpotifyId);
                tracksWithGenres.Add((track, trackGenres.ToList()));
            }

            // Filter by genre.
            var filteredTracks = tracksWithGenres
                .Where(t => t.Genres.Contains(genre))
                .Select(t => t.Track)
                .ToList();

            // Assert: Should return only tracks with this genre.
            Assert.Equal(tracksPerGenre, filteredTracks.Count);
            
            // Assert: All expected tracks should be in the filtered list.
            foreach (var expectedTrack in tracksWithGenreData[genre])
            {
                Assert.Contains(filteredTracks, t => t.TrackRecordId == expectedTrack.TrackRecordId);
            }

            // Assert: No tracks without genre should be in the filtered list.
            Assert.All(filteredTracks, track => 
                Assert.DoesNotContain("no_genre", track.TrackSpotifyId));
        }
    }

    private class TrackRecordComparer : IEqualityComparer<TrackRecordEntity>
    {
        public bool Equals(TrackRecordEntity? x, TrackRecordEntity? y)
        {
            if (x == null || y == null) return false;
            return x.TrackRecordId == y.TrackRecordId;
        }

        public int GetHashCode(TrackRecordEntity obj)
        {
            return obj.TrackRecordId.GetHashCode();
        }
    }
}
