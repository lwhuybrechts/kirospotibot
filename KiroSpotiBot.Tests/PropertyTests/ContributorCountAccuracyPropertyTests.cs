using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property 36: Contributor Count Accuracy
/// For any contributor displayed in the web frontend, the track count should equal 
/// the number of tracks they actually shared in that playlist.
/// Validates: Requirements 16.2
/// </summary>
public class ContributorCountAccuracyPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ITrackRecordRepository _trackRecordRepository;

    public ContributorCountAccuracyPropertyTests()
    {
        var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
            ?? "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        // Truncate table at the beginning of each test.
        TableHelper.TruncateTable(_tableServiceClient, "TrackRecords");
        
        _trackRecordRepository = new TrackRecordRepository(
            _tableServiceClient,
            NullLogger<BaseRepository<TrackRecordEntity>>.Instance
        );
    }

    [Theory]
    [InlineData(1, 5, 3, 2)] // Chat 1: User 1 shares 5, User 2 shares 3, User 3 shares 2.
    [InlineData(2, 10, 7, 5)] // Chat 2: User 1 shares 10, User 2 shares 7, User 3 shares 5.
    [InlineData(3, 1, 1, 1)] // Chat 3: Each user shares 1 track.
    public async Task ContributorCount_ShouldMatchActualTracksShared(
        long chatId,
        int user1TrackCount,
        int user2TrackCount,
        int user3TrackCount)
    {
        // Arrange: Create tracks shared by multiple users.
        var user1Id = 3001L;
        var user2Id = 3002L;
        var user3Id = 3003L;

        // Create tracks for user 1.
        for (int i = 0; i < user1TrackCount; i++)
        {
            var track = new TrackRecordEntity(chatId, $"track_u1_{i}", user1Id)
            {
                TrackName = $"Track {i} by User 1",
                ArtistName = "Artist 1",
                AlbumName = "Album 1",
                SharedByUsername = "User1",
                SharedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
        }

        // Create tracks for user 2.
        for (int i = 0; i < user2TrackCount; i++)
        {
            var track = new TrackRecordEntity(chatId, $"track_u2_{i}", user2Id)
            {
                TrackName = $"Track {i} by User 2",
                ArtistName = "Artist 2",
                AlbumName = "Album 2",
                SharedByUsername = "User2",
                SharedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
        }

        // Create tracks for user 3.
        for (int i = 0; i < user3TrackCount; i++)
        {
            var track = new TrackRecordEntity(chatId, $"track_u3_{i}", user3Id)
            {
                TrackName = $"Track {i} by User 3",
                ArtistName = "Artist 3",
                AlbumName = "Album 3",
                SharedByUsername = "User3",
                SharedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
        }

        // Act: Get contributors for the playlist.
        var contributors = await _trackRecordRepository.GetContributorsAsync(chatId);
        var contributorsList = contributors.ToList();

        // Assert: Should have 3 contributors.
        Assert.Equal(3, contributorsList.Count);

        // Assert: Each contributor's track count should match actual tracks shared.
        var user1Contributor = contributorsList.First(c => c.TelegramUserId == user1Id);
        Assert.Equal(user1TrackCount, user1Contributor.TrackCount);
        Assert.Equal("User1", user1Contributor.Username);

        var user2Contributor = contributorsList.First(c => c.TelegramUserId == user2Id);
        Assert.Equal(user2TrackCount, user2Contributor.TrackCount);
        Assert.Equal("User2", user2Contributor.Username);

        var user3Contributor = contributorsList.First(c => c.TelegramUserId == user3Id);
        Assert.Equal(user3TrackCount, user3Contributor.TrackCount);
        Assert.Equal("User3", user3Contributor.Username);
    }

    [Theory]
    [InlineData(1, 10, 3)] // User shares 10 tracks, 3 are deleted.
    [InlineData(2, 5, 2)] // User shares 5 tracks, 2 are deleted.
    public async Task ContributorCount_ShouldExcludeDeletedTracks(
        long chatId,
        int totalTracks,
        int deletedCount)
    {
        // Arrange: Create tracks, some deleted.
        var userId = 4001L;

        for (int i = 0; i < totalTracks; i++)
        {
            var track = new TrackRecordEntity(chatId, $"track_{i}", userId)
            {
                TrackName = $"Track {i}",
                ArtistName = "Artist",
                AlbumName = "Album",
                SharedByUsername = "TestUser",
                IsDeleted = i < deletedCount // First N tracks are deleted.
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
        }

        // Act: Get contributors for the playlist.
        var contributors = await _trackRecordRepository.GetContributorsAsync(chatId);
        var contributorsList = contributors.ToList();

        // Assert: Should have 1 contributor.
        Assert.Single(contributorsList);

        // Assert: Track count should exclude deleted tracks.
        var contributor = contributorsList.First();
        Assert.Equal(userId, contributor.TelegramUserId);
        Assert.Equal(totalTracks - deletedCount, contributor.TrackCount);
        Assert.Equal("TestUser", contributor.Username);
    }

    [Theory]
    [InlineData(1)] // Empty playlist.
    [InlineData(2)] // Another empty playlist.
    public async Task ContributorCount_ShouldReturnEmptyForEmptyPlaylist(long chatId)
    {
        // Act: Get contributors for an empty playlist.
        var contributors = await _trackRecordRepository.GetContributorsAsync(chatId);
        var contributorsList = contributors.ToList();

        // Assert: Should have no contributors.
        Assert.Empty(contributorsList);
    }
}
