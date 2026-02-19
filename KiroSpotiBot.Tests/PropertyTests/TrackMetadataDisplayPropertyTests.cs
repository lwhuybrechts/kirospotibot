using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property 34: Web Frontend Track Metadata Display
/// For any track displayed in the web frontend, all metadata fields (name, artist, album, genre),
/// sharing information (who shared it, when), and vote counts should be visible.
/// Validates: Requirements 15.3, 15.4, 15.5
/// </summary>
[Trait("Feature", "telegram-spotify-bot")]
[Trait("Property", "Property 34: Web Frontend Track Metadata Display")]
public class TrackMetadataDisplayPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly ITrackGenreRepository _trackGenreRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly TableClient _trackRecordsTableClient;
    private readonly TableClient _trackGenresTableClient;
    private readonly TableClient _votesTableClient;

    public TrackMetadataDisplayPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        // Create repositories.
        var trackRecordLogger = Mock.Of<ILogger<BaseRepository<TrackRecordEntity>>>();
        _trackRecordRepository = new TrackRecordRepository(_tableServiceClient, trackRecordLogger);
        
        var trackGenreLogger = Mock.Of<ILogger<TrackGenreRepository>>();
        _trackGenreRepository = new TrackGenreRepository(_tableServiceClient, trackGenreLogger);
        
        var voteLogger = Mock.Of<ILogger<BaseRepository<VoteEntity>>>();
        _voteRepository = new VoteRepository(_tableServiceClient, voteLogger);
        
        // Get table clients and truncate.
        _trackRecordsTableClient = _tableServiceClient.GetTableClient("TrackRecords");
        _trackGenresTableClient = _tableServiceClient.GetTableClient("TrackGenres");
        _votesTableClient = _tableServiceClient.GetTableClient("Votes");
        
        TableHelper.TruncateTable(_trackRecordsTableClient);
        TableHelper.TruncateTable(_trackGenresTableClient);
        TableHelper.TruncateTable(_votesTableClient);
    }

    [Theory]
    [InlineData("Track 1", "Artist 1", "Album 1", "https://example.com/album1.jpg", "User1", "https://example.com/avatar1.jpg", 5, 2)]
    [InlineData("Track 2", "Artist 2", "Album 2", null, "User2", null, 0, 0)]
    [InlineData("Track 3", "Artist 3", "Album 3", "https://example.com/album3.jpg", "User3", "https://example.com/avatar3.jpg", 10, 1)]
    public async Task TrackMetadataDisplay_AllFieldsAreVisible(
        string trackName,
        string artistName,
        string albumName,
        string? albumImageUrl,
        string sharedByUsername,
        string? sharedByAvatarUrl,
        int upvoteCount,
        int downvoteCount)
    {
        // Arrange: Create a track record with all metadata.
        var telegramChatId = 12345L;
        var sharedByUserId = 67890L;
        var trackSpotifyId = Guid.NewGuid().ToString();

        var trackRecord = new TrackRecordEntity(telegramChatId, trackSpotifyId, sharedByUserId)
        {
            TrackName = trackName,
            ArtistName = artistName,
            AlbumName = albumName,
            AlbumImageUrl = albumImageUrl,
            SharedByUsername = sharedByUsername,
            SharedByAvatarUrl = sharedByAvatarUrl,
            UpvoteCount = upvoteCount,
            DownvoteCount = downvoteCount,
            SharedAt = DateTime.UtcNow
        };

        await _trackRecordRepository.CreateTrackRecordAsync(trackRecord);

        // Add genres.
        await _trackGenreRepository.CreateAsync(trackSpotifyId, "Rock");
        await _trackGenreRepository.CreateAsync(trackSpotifyId, "Pop");

        // Add votes.
        for (int i = 0; i < upvoteCount; i++)
        {
            var vote = new VoteEntity(trackRecord.TrackRecordId, 1000L + i, "Upvote")
            {
                VoterUsername = $"Voter{i}",
                VoterAvatarUrl = $"https://example.com/voter{i}.jpg"
            };
            await _voteRepository.UpsertVoteAsync(vote);
        }

        for (int i = 0; i < downvoteCount; i++)
        {
            var vote = new VoteEntity(trackRecord.TrackRecordId, 2000L + i, "Downvote")
            {
                VoterUsername = $"Downvoter{i}",
                VoterAvatarUrl = $"https://example.com/downvoter{i}.jpg"
            };
            await _voteRepository.UpsertVoteAsync(vote);
        }

        // Act: Retrieve the track record (simulating what the web frontend would do).
        var retrievedTrack = await _trackRecordRepository.GetByIdAsync(trackRecord.TrackRecordId, telegramChatId);
        var genres = await _trackGenreRepository.GetGenresForTrackAsync(trackSpotifyId);
        var votes = await _voteRepository.GetByTrackRecordAsync(trackRecord.TrackRecordId);

        // Assert: Verify all metadata fields are present and correct.
        Assert.NotNull(retrievedTrack);
        
        // Track metadata.
        Assert.Equal(trackName, retrievedTrack.TrackName);
        Assert.Equal(artistName, retrievedTrack.ArtistName);
        Assert.Equal(albumName, retrievedTrack.AlbumName);
        Assert.Equal(albumImageUrl, retrievedTrack.AlbumImageUrl);
        
        // Sharing information.
        Assert.Equal(sharedByUserId, retrievedTrack.SharedByTelegramUserId);
        Assert.Equal(sharedByUsername, retrievedTrack.SharedByUsername);
        Assert.Equal(sharedByAvatarUrl, retrievedTrack.SharedByAvatarUrl);
        Assert.True(retrievedTrack.SharedAt <= DateTime.UtcNow);
        
        // Vote counts.
        Assert.Equal(upvoteCount, retrievedTrack.UpvoteCount);
        Assert.Equal(downvoteCount, retrievedTrack.DownvoteCount);
        
        // Genres.
        Assert.Equal(2, genres.Count());
        Assert.Contains("Rock", genres);
        Assert.Contains("Pop", genres);
        
        // Votes with voter information.
        Assert.Equal(upvoteCount + downvoteCount, votes.Count());
        Assert.Equal(upvoteCount, votes.Count(v => v.VoteType == "Upvote"));
        Assert.Equal(downvoteCount, votes.Count(v => v.VoteType == "Downvote"));
        
        // Verify voter information is present.
        foreach (var vote in votes)
        {
            Assert.NotEmpty(vote.VoterUsername);
            Assert.NotNull(vote.VoterAvatarUrl);
        }
    }

    [Theory]
    [InlineData("Track A", "Artist A", "Album A")]
    [InlineData("Track B", "Artist B", "Album B")]
    public async Task TrackMetadataDisplay_WithoutGenres_AllOtherFieldsAreVisible(
        string trackName,
        string artistName,
        string albumName)
    {
        // Arrange: Create a track record without genres.
        var telegramChatId = 12345L;
        var sharedByUserId = 67890L;
        var trackSpotifyId = Guid.NewGuid().ToString();

        var trackRecord = new TrackRecordEntity(telegramChatId, trackSpotifyId, sharedByUserId)
        {
            TrackName = trackName,
            ArtistName = artistName,
            AlbumName = albumName,
            SharedByUsername = "TestUser",
            UpvoteCount = 3,
            DownvoteCount = 1,
            SharedAt = DateTime.UtcNow
        };

        await _trackRecordRepository.CreateTrackRecordAsync(trackRecord);

        // Act: Retrieve the track record.
        var retrievedTrack = await _trackRecordRepository.GetByIdAsync(trackRecord.TrackRecordId, telegramChatId);
        var genres = await _trackGenreRepository.GetGenresForTrackAsync(trackSpotifyId);

        // Assert: Verify all metadata fields except genres are present.
        Assert.NotNull(retrievedTrack);
        Assert.Equal(trackName, retrievedTrack.TrackName);
        Assert.Equal(artistName, retrievedTrack.ArtistName);
        Assert.Equal(albumName, retrievedTrack.AlbumName);
        Assert.Equal("TestUser", retrievedTrack.SharedByUsername);
        Assert.Equal(3, retrievedTrack.UpvoteCount);
        Assert.Equal(1, retrievedTrack.DownvoteCount);
        
        // Genres should be empty but not null.
        Assert.Empty(genres);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 0)]
    [InlineData(0, 3)]
    [InlineData(10, 5)]
    public async Task TrackMetadataDisplay_VoteCountsAreAccurate(int upvoteCount, int downvoteCount)
    {
        // Arrange: Create a track record with specific vote counts.
        var telegramChatId = 12345L;
        var sharedByUserId = 67890L;
        var trackSpotifyId = Guid.NewGuid().ToString();

        var trackRecord = new TrackRecordEntity(telegramChatId, trackSpotifyId, sharedByUserId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            AlbumName = "Test Album",
            SharedByUsername = "TestUser",
            UpvoteCount = upvoteCount,
            DownvoteCount = downvoteCount,
            SharedAt = DateTime.UtcNow
        };

        await _trackRecordRepository.CreateTrackRecordAsync(trackRecord);

        // Add actual votes to match the counts.
        for (int i = 0; i < upvoteCount; i++)
        {
            var vote = new VoteEntity(trackRecord.TrackRecordId, 1000L + i, "Upvote")
            {
                VoterUsername = $"Upvoter{i}"
            };
            await _voteRepository.UpsertVoteAsync(vote);
        }

        for (int i = 0; i < downvoteCount; i++)
        {
            var vote = new VoteEntity(trackRecord.TrackRecordId, 2000L + i, "Downvote")
            {
                VoterUsername = $"Downvoter{i}"
            };
            await _voteRepository.UpsertVoteAsync(vote);
        }

        // Act: Retrieve the track and votes.
        var retrievedTrack = await _trackRecordRepository.GetByIdAsync(trackRecord.TrackRecordId, telegramChatId);
        var votes = await _voteRepository.GetByTrackRecordAsync(trackRecord.TrackRecordId);

        // Assert: Verify vote counts match.
        Assert.NotNull(retrievedTrack);
        Assert.Equal(upvoteCount, retrievedTrack.UpvoteCount);
        Assert.Equal(downvoteCount, retrievedTrack.DownvoteCount);
        Assert.Equal(upvoteCount + downvoteCount, votes.Count());
    }
}
