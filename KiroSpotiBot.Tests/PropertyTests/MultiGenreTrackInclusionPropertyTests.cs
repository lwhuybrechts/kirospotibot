using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property 38: Multi-Genre Track Inclusion
/// For any track with multiple genres, it should appear in the filtered results 
/// when any of its genres is selected.
/// Validates: Requirements 17.5
/// </summary>
public class MultiGenreTrackInclusionPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly ITrackGenreRepository _trackGenreRepository;

    public MultiGenreTrackInclusionPropertyTests()
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
    [InlineData(1, 2)] // Track with 2 genres.
    [InlineData(2, 3)] // Track with 3 genres.
    [InlineData(3, 5)] // Track with 5 genres.
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 38: Multi-Genre Track Inclusion")]
    public async Task MultiGenreTrack_ShouldAppearInAllGenreFilters(long chatId, int genreCount)
    {
        // Arrange: Create a track with multiple genres.
        var trackId = "multi_genre_track";
        var track = new TrackRecordEntity(chatId, trackId, 1001L)
        {
            TrackName = "Multi-Genre Track",
            ArtistName = "Versatile Artist",
            AlbumName = "Eclectic Album",
            SharedByUsername = "User1",
            SharedAt = DateTime.UtcNow
        };
        await _trackRecordRepository.CreateTrackRecordAsync(track);

        // Add multiple genres to the track.
        var genres = new List<string>();
        for (int i = 0; i < genreCount; i++)
        {
            var genre = $"genre_{i}";
            genres.Add(genre);
            await _trackGenreRepository.CreateAsync(trackId, genre);
        }

        // Act & Assert: Filter by each genre and verify track appears.
        var allTracks = await _trackRecordRepository.GetByGroupChatAsync(chatId, 0, 100);
        var allTracksList = allTracks.ToList();

        foreach (var genre in genres)
        {
            // Load genres for each track.
            var tracksWithGenres = new List<(TrackRecordEntity Track, List<string> Genres)>();
            foreach (var t in allTracksList)
            {
                var trackGenres = await _trackGenreRepository.GetGenresForTrackAsync(t.TrackSpotifyId);
                tracksWithGenres.Add((t, trackGenres.ToList()));
            }

            // Filter by this genre.
            var filteredTracks = tracksWithGenres
                .Where(t => t.Genres.Contains(genre))
                .Select(t => t.Track)
                .ToList();

            // Assert: Track should appear in the filtered results.
            Assert.Single(filteredTracks);
            Assert.Equal(track.TrackRecordId, filteredTracks[0].TrackRecordId);
        }
    }

    [Theory]
    [InlineData(1, 3, 2)] // 3 tracks: 1 with multiple genres, 2 with single genre.
    [InlineData(2, 5, 3)] // 5 tracks: 2 with multiple genres, 3 with single genre.
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 38: Multi-Genre Track Inclusion")]
    public async Task MultiGenreTrack_ShouldAppearAlongsideSingleGenreTracks(
        long chatId,
        int totalTracks,
        int multiGenreCount)
    {
        // Arrange: Create tracks with different genre configurations.
        var rockGenre = "rock";
        var popGenre = "pop";
        
        var multiGenreTracks = new List<TrackRecordEntity>();
        var singleGenreTracks = new List<TrackRecordEntity>();

        // Create multi-genre tracks (rock + pop).
        for (int i = 0; i < multiGenreCount; i++)
        {
            var trackId = $"multi_track_{i}";
            var track = new TrackRecordEntity(chatId, trackId, 2001L)
            {
                TrackName = $"Multi-Genre Track {i}",
                ArtistName = "Artist",
                AlbumName = "Album",
                SharedByUsername = "User"
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            await _trackGenreRepository.CreateAsync(trackId, rockGenre);
            await _trackGenreRepository.CreateAsync(trackId, popGenre);
            multiGenreTracks.Add(track);
        }

        // Create single-genre tracks (rock only).
        var singleGenreCount = totalTracks - multiGenreCount;
        for (int i = 0; i < singleGenreCount; i++)
        {
            var trackId = $"single_track_{i}";
            var track = new TrackRecordEntity(chatId, trackId, 2002L)
            {
                TrackName = $"Single-Genre Track {i}",
                ArtistName = "Artist",
                AlbumName = "Album",
                SharedByUsername = "User"
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            await _trackGenreRepository.CreateAsync(trackId, rockGenre);
            singleGenreTracks.Add(track);
        }

        // Act: Get all tracks and filter by rock genre.
        var allTracks = await _trackRecordRepository.GetByGroupChatAsync(chatId, 0, 100);
        var allTracksList = allTracks.ToList();

        // Load genres for each track.
        var tracksWithGenres = new List<(TrackRecordEntity Track, List<string> Genres)>();
        foreach (var track in allTracksList)
        {
            var trackGenres = await _trackGenreRepository.GetGenresForTrackAsync(track.TrackSpotifyId);
            tracksWithGenres.Add((track, trackGenres.ToList()));
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

        // Assert: Rock filter should include both multi-genre and single-genre tracks.
        Assert.Equal(totalTracks, rockFilteredTracks.Count);
        
        // Assert: All multi-genre tracks should be in rock filter.
        foreach (var expectedTrack in multiGenreTracks)
        {
            Assert.Contains(rockFilteredTracks, t => t.TrackRecordId == expectedTrack.TrackRecordId);
        }
        
        // Assert: All single-genre tracks should be in rock filter.
        foreach (var expectedTrack in singleGenreTracks)
        {
            Assert.Contains(rockFilteredTracks, t => t.TrackRecordId == expectedTrack.TrackRecordId);
        }

        // Assert: Pop filter should include only multi-genre tracks.
        Assert.Equal(multiGenreCount, popFilteredTracks.Count);
        
        // Assert: All multi-genre tracks should be in pop filter.
        foreach (var expectedTrack in multiGenreTracks)
        {
            Assert.Contains(popFilteredTracks, t => t.TrackRecordId == expectedTrack.TrackRecordId);
        }
        
        // Assert: No single-genre tracks should be in pop filter.
        foreach (var singleTrack in singleGenreTracks)
        {
            Assert.DoesNotContain(popFilteredTracks, t => t.TrackRecordId == singleTrack.TrackRecordId);
        }
    }

    [Theory]
    [InlineData(1, 4)] // 4 tracks with overlapping genres.
    [InlineData(2, 6)] // 6 tracks with overlapping genres.
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 38: Multi-Genre Track Inclusion")]
    public async Task MultiGenreTrack_ShouldHandleOverlappingGenres(long chatId, int trackCount)
    {
        // Arrange: Create tracks with various genre combinations.
        // Track 0: rock
        // Track 1: rock, pop
        // Track 2: rock, pop, jazz
        // Track 3: pop, jazz
        // etc.
        
        var rockGenre = "rock";
        var popGenre = "pop";
        var jazzGenre = "jazz";
        
        var tracks = new List<TrackRecordEntity>();
        var trackGenreMap = new Dictionary<string, List<string>>();

        for (int i = 0; i < trackCount; i++)
        {
            var trackId = $"track_{i}";
            var track = new TrackRecordEntity(chatId, trackId, 3001L)
            {
                TrackName = $"Track {i}",
                ArtistName = "Artist",
                AlbumName = "Album",
                SharedByUsername = "User"
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            tracks.Add(track);

            var trackGenres = new List<string>();
            
            // Assign genres based on track index.
            if (i % 4 == 0)
            {
                // Rock only.
                trackGenres.Add(rockGenre);
                await _trackGenreRepository.CreateAsync(trackId, rockGenre);
            }
            else if (i % 4 == 1)
            {
                // Rock + Pop.
                trackGenres.Add(rockGenre);
                trackGenres.Add(popGenre);
                await _trackGenreRepository.CreateAsync(trackId, rockGenre);
                await _trackGenreRepository.CreateAsync(trackId, popGenre);
            }
            else if (i % 4 == 2)
            {
                // Rock + Pop + Jazz.
                trackGenres.Add(rockGenre);
                trackGenres.Add(popGenre);
                trackGenres.Add(jazzGenre);
                await _trackGenreRepository.CreateAsync(trackId, rockGenre);
                await _trackGenreRepository.CreateAsync(trackId, popGenre);
                await _trackGenreRepository.CreateAsync(trackId, jazzGenre);
            }
            else
            {
                // Pop + Jazz.
                trackGenres.Add(popGenre);
                trackGenres.Add(jazzGenre);
                await _trackGenreRepository.CreateAsync(trackId, popGenre);
                await _trackGenreRepository.CreateAsync(trackId, jazzGenre);
            }
            
            trackGenreMap[trackId] = trackGenres;
        }

        // Act: Get all tracks and filter by each genre.
        var allTracks = await _trackRecordRepository.GetByGroupChatAsync(chatId, 0, 100);
        var allTracksList = allTracks.ToList();

        // Load genres for each track.
        var tracksWithGenres = new List<(TrackRecordEntity Track, List<string> Genres)>();
        foreach (var track in allTracksList)
        {
            var trackGenres = await _trackGenreRepository.GetGenresForTrackAsync(track.TrackSpotifyId);
            tracksWithGenres.Add((track, trackGenres.ToList()));
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

        // Filter by jazz genre.
        var jazzFilteredTracks = tracksWithGenres
            .Where(t => t.Genres.Contains(jazzGenre))
            .Select(t => t.Track)
            .ToList();

        // Assert: Each track should appear in filters for all its genres.
        foreach (var track in tracks)
        {
            var expectedGenres = trackGenreMap[track.TrackSpotifyId];
            
            if (expectedGenres.Contains(rockGenre))
            {
                Assert.Contains(rockFilteredTracks, t => t.TrackRecordId == track.TrackRecordId);
            }
            else
            {
                Assert.DoesNotContain(rockFilteredTracks, t => t.TrackRecordId == track.TrackRecordId);
            }
            
            if (expectedGenres.Contains(popGenre))
            {
                Assert.Contains(popFilteredTracks, t => t.TrackRecordId == track.TrackRecordId);
            }
            else
            {
                Assert.DoesNotContain(popFilteredTracks, t => t.TrackRecordId == track.TrackRecordId);
            }
            
            if (expectedGenres.Contains(jazzGenre))
            {
                Assert.Contains(jazzFilteredTracks, t => t.TrackRecordId == track.TrackRecordId);
            }
            else
            {
                Assert.DoesNotContain(jazzFilteredTracks, t => t.TrackRecordId == track.TrackRecordId);
            }
        }
    }
}
