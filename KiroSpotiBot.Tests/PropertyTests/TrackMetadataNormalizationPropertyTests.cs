using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for track metadata normalization.
/// Property 19: Track Metadata Normalization
/// Validates: Requirements 11.5
/// 
/// Note: These tests verify that track metadata is stored once and referenced
/// by all track records, not duplicated across playlists.
/// </summary>
public class TrackMetadataNormalizationPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ITrackRepository _trackRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly IGenreRepository _genreRepository;
    private readonly ITrackGenreRepository _trackGenreRepository;
    private readonly TableClient _tracksTable;
    private readonly TableClient _artistsTable;
    private readonly TableClient _albumsTable;
    private readonly TableClient _genresTable;
    private readonly TableClient _trackGenresTable;

    public TrackMetadataNormalizationPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var trackLogger = Mock.Of<ILogger<TrackRepository>>();
        var artistLogger = Mock.Of<ILogger<ArtistRepository>>();
        var albumLogger = Mock.Of<ILogger<AlbumRepository>>();
        var genreLogger = Mock.Of<ILogger<GenreRepository>>();
        var trackGenreLogger = Mock.Of<ILogger<TrackGenreRepository>>();
        
        _trackRepository = new TrackRepository(_tableServiceClient, trackLogger);
        _artistRepository = new ArtistRepository(_tableServiceClient, artistLogger);
        _albumRepository = new AlbumRepository(_tableServiceClient, albumLogger);
        _genreRepository = new GenreRepository(_tableServiceClient, genreLogger);
        _trackGenreRepository = new TrackGenreRepository(_tableServiceClient, trackGenreLogger);
        
        // Get table references.
        _tracksTable = _tableServiceClient.GetTableClient("Tracks");
        _artistsTable = _tableServiceClient.GetTableClient("Artists");
        _albumsTable = _tableServiceClient.GetTableClient("Albums");
        _genresTable = _tableServiceClient.GetTableClient("Genres");
        _trackGenresTable = _tableServiceClient.GetTableClient("TrackGenres");
        
        // Truncate tables before tests.
        TableHelper.TruncateTable(_tracksTable);
        TableHelper.TruncateTable(_artistsTable);
        TableHelper.TruncateTable(_albumsTable);
        TableHelper.TruncateTable(_genresTable);
        TableHelper.TruncateTable(_trackGenresTable);
    }

    [Theory]
    [InlineData("3n3Ppam7vgaVa1iaRUc9Lp", "Mr. Brightside", "The Killers", "Hot Fuss")]
    [InlineData("7qiZfU4dY1lWllzX7mPBI", "Bohemian Rhapsody", "Queen", "A Night at the Opera")]
    [InlineData("0VjIjW4GlUZAMYd2vXMi3b", "Stairway to Heaven", "Led Zeppelin", "Led Zeppelin IV")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 19: Track Metadata Normalization")]
    public async Task MetadataNormalization_TrackStoredOnce_ReferencedMultipleTimes(
        string trackId,
        string trackName,
        string artistName,
        string albumName)
    {
        // Arrange: Create metadata.
        var metadata = new SpotifyTrackMetadata(
            SpotifyId: trackId,
            Name: trackName,
            DurationSeconds: 240,
            PreviewUrl: "https://preview.url",
            ArtistSpotifyId: "artist123",
            ArtistName: artistName,
            AlbumSpotifyId: "album123",
            AlbumName: albumName,
            AlbumImageUrl: "https://image.url",
            Genres: new List<string> { "rock", "alternative" }.AsReadOnly()
        );
        
        // Act: Store track metadata first time.
        var track1 = await _trackRepository.GetOrCreateAsync(trackId, metadata, CancellationToken.None);
        
        // Store same track metadata second time (simulating different playlist).
        var track2 = await _trackRepository.GetOrCreateAsync(trackId, metadata, CancellationToken.None);
        
        // Assert: Both references should point to same track entity.
        Assert.Equal(track1.SpotifyId, track2.SpotifyId);
        Assert.Equal(track1.Name, track2.Name);
        Assert.Equal(track1.ArtistName, track2.ArtistName);
        Assert.Equal(track1.AlbumName, track2.AlbumName);
        
        // Verify only one track entity exists in storage.
        var allTracks = new List<TrackEntity>();
        await foreach (var track in _tracksTable.QueryAsync<TrackEntity>(filter: $"PartitionKey eq 'TRACK'"))
        {
            if (track.SpotifyId == trackId)
            {
                allTracks.Add(track);
            }
        }
        Assert.Single(allTracks);
    }

    [Theory]
    [InlineData("artist123", "The Killers")]
    [InlineData("artist456", "Queen")]
    [InlineData("artist789", "Led Zeppelin")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 19: Track Metadata Normalization")]
    public async Task MetadataNormalization_ArtistStoredOnce_ReferencedByMultipleTracks(
        string artistId,
        string artistName)
    {
        // Act: Store artist first time.
        var artist1 = await _artistRepository.GetOrCreateAsync(artistId, artistName, CancellationToken.None);
        
        // Store same artist second time (simulating different track).
        var artist2 = await _artistRepository.GetOrCreateAsync(artistId, artistName, CancellationToken.None);
        
        // Assert: Both references should point to same artist entity.
        Assert.Equal(artist1.SpotifyId, artist2.SpotifyId);
        Assert.Equal(artist1.Name, artist2.Name);
        
        // Verify only one artist entity exists in storage.
        var allArtists = new List<ArtistEntity>();
        await foreach (var artist in _artistsTable.QueryAsync<ArtistEntity>(filter: $"PartitionKey eq 'ARTIST'"))
        {
            if (artist.SpotifyId == artistId)
            {
                allArtists.Add(artist);
            }
        }
        Assert.Single(allArtists);
    }

    [Theory]
    [InlineData("album123", "Hot Fuss", "https://image1.url")]
    [InlineData("album456", "A Night at the Opera", "https://image2.url")]
    [InlineData("album789", "Led Zeppelin IV", null)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 19: Track Metadata Normalization")]
    public async Task MetadataNormalization_AlbumStoredOnce_ReferencedByMultipleTracks(
        string albumId,
        string albumName,
        string? imageUrl)
    {
        // Act: Store album first time.
        var album1 = await _albumRepository.GetOrCreateAsync(albumId, albumName, imageUrl, CancellationToken.None);
        
        // Store same album second time (simulating different track).
        var album2 = await _albumRepository.GetOrCreateAsync(albumId, albumName, imageUrl, CancellationToken.None);
        
        // Assert: Both references should point to same album entity.
        Assert.Equal(album1.SpotifyId, album2.SpotifyId);
        Assert.Equal(album1.Name, album2.Name);
        Assert.Equal(album1.ImageUrl, album2.ImageUrl);
        
        // Verify only one album entity exists in storage.
        var allAlbums = new List<AlbumEntity>();
        await foreach (var album in _albumsTable.QueryAsync<AlbumEntity>(filter: $"PartitionKey eq 'ALBUM'"))
        {
            if (album.SpotifyId == albumId)
            {
                allAlbums.Add(album);
            }
        }
        Assert.Single(allAlbums);
    }

    [Theory]
    [InlineData("rock")]
    [InlineData("alternative")]
    [InlineData("indie")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 19: Track Metadata Normalization")]
    public async Task MetadataNormalization_GenreStoredOnce_ReferencedByMultipleTracks(
        string genreName)
    {
        // Act: Store genre first time.
        var genre1 = await _genreRepository.GetOrCreateAsync(genreName, CancellationToken.None);
        
        // Store same genre second time (simulating different track).
        var genre2 = await _genreRepository.GetOrCreateAsync(genreName, CancellationToken.None);
        
        // Assert: Both references should point to same genre entity.
        Assert.Equal(genre1.GenreName, genre2.GenreName);
        
        // Verify only one genre entity exists in storage.
        var allGenres = new List<GenreEntity>();
        await foreach (var genre in _genresTable.QueryAsync<GenreEntity>(filter: $"PartitionKey eq 'GENRE'"))
        {
            if (genre.GenreName == genreName)
            {
                allGenres.Add(genre);
            }
        }
        Assert.Single(allGenres);
    }

    [Theory]
    [InlineData("3n3Ppam7vgaVa1iaRUc9Lp", "Mr. Brightside", "The Killers", "Hot Fuss", "rock", "alternative")]
    [InlineData("7qiZfU4dY1lWllzX7mPBI", "Bohemian Rhapsody", "Queen", "A Night at the Opera", "rock", "classic rock")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 19: Track Metadata Normalization")]
    public async Task MetadataNormalization_CompleteMetadata_AllEntitiesNormalized(
        string trackId,
        string trackName,
        string artistName,
        string albumName,
        string genre1,
        string genre2)
    {
        // Arrange: Create complete metadata.
        var metadata = new SpotifyTrackMetadata(
            SpotifyId: trackId,
            Name: trackName,
            DurationSeconds: 240,
            PreviewUrl: "https://preview.url",
            ArtistSpotifyId: "artist123",
            ArtistName: artistName,
            AlbumSpotifyId: "album123",
            AlbumName: albumName,
            AlbumImageUrl: "https://image.url",
            Genres: new List<string> { genre1, genre2 }.AsReadOnly()
        );
        
        // Act: Store all metadata.
        await _trackRepository.GetOrCreateAsync(trackId, metadata, CancellationToken.None);
        await _artistRepository.GetOrCreateAsync("artist123", artistName, CancellationToken.None);
        await _albumRepository.GetOrCreateAsync("album123", albumName, "https://image.url", CancellationToken.None);
        await _genreRepository.GetOrCreateAsync(genre1, CancellationToken.None);
        await _genreRepository.GetOrCreateAsync(genre2, CancellationToken.None);
        
        // Store same metadata again (simulating second playlist).
        await _trackRepository.GetOrCreateAsync(trackId, metadata, CancellationToken.None);
        await _artistRepository.GetOrCreateAsync("artist123", artistName, CancellationToken.None);
        await _albumRepository.GetOrCreateAsync("album123", albumName, "https://image.url", CancellationToken.None);
        await _genreRepository.GetOrCreateAsync(genre1, CancellationToken.None);
        await _genreRepository.GetOrCreateAsync(genre2, CancellationToken.None);
        
        // Assert: Verify only one of each entity exists.
        var trackCount = 0;
        await foreach (var _ in _tracksTable.QueryAsync<TrackEntity>(filter: $"PartitionKey eq 'TRACK' and RowKey eq '{trackId}'"))
        {
            trackCount++;
        }
        Assert.Equal(1, trackCount);
        
        var artistCount = 0;
        await foreach (var _ in _artistsTable.QueryAsync<ArtistEntity>(filter: $"PartitionKey eq 'ARTIST' and RowKey eq 'artist123'"))
        {
            artistCount++;
        }
        Assert.Equal(1, artistCount);
        
        var albumCount = 0;
        await foreach (var _ in _albumsTable.QueryAsync<AlbumEntity>(filter: $"PartitionKey eq 'ALBUM' and RowKey eq 'album123'"))
        {
            albumCount++;
        }
        Assert.Equal(1, albumCount);
        
        var genre1Count = 0;
        await foreach (var _ in _genresTable.QueryAsync<GenreEntity>(filter: $"PartitionKey eq 'GENRE' and RowKey eq '{genre1}'"))
        {
            genre1Count++;
        }
        Assert.Equal(1, genre1Count);
        
        var genre2Count = 0;
        await foreach (var _ in _genresTable.QueryAsync<GenreEntity>(filter: $"PartitionKey eq 'GENRE' and RowKey eq '{genre2}'"))
        {
            genre2Count++;
        }
        Assert.Equal(1, genre2Count);
    }
}
