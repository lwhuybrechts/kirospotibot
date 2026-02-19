using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property 35: User Filter Accuracy
/// For any contributor selected in the web frontend, the filtered track list should contain 
/// only tracks shared by that user and should include all tracks they shared.
/// Validates: Requirements 16.3
/// </summary>
public class UserFilterAccuracyPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ITrackRecordRepository _trackRecordRepository;

    public UserFilterAccuracyPropertyTests()
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
    [InlineData(1, 5, 3)] // User 1 shares 5 tracks, User 2 shares 3 tracks.
    [InlineData(2, 10, 7)] // User 1 shares 10 tracks, User 2 shares 7 tracks.
    [InlineData(3, 1, 1)] // User 1 shares 1 track, User 2 shares 1 track.
    public async Task UserFilter_ShouldReturnOnlyTracksSharedBySelectedUser(
        long chatId,
        int user1TrackCount,
        int user2TrackCount)
    {
        // Arrange: Create tracks shared by two different users.
        var user1Id = 1001L;
        var user2Id = 1002L;
        
        var user1Tracks = new List<TrackRecordEntity>();
        var user2Tracks = new List<TrackRecordEntity>();

        // Create tracks for user 1.
        for (int i = 0; i < user1TrackCount; i++)
        {
            var track = new TrackRecordEntity(chatId, $"track_user1_{i}", user1Id)
            {
                TrackName = $"Track {i} by User 1",
                ArtistName = "Artist 1",
                AlbumName = "Album 1",
                SharedByUsername = "User1",
                SharedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            user1Tracks.Add(track);
        }

        // Create tracks for user 2.
        for (int i = 0; i < user2TrackCount; i++)
        {
            var track = new TrackRecordEntity(chatId, $"track_user2_{i}", user2Id)
            {
                TrackName = $"Track {i} by User 2",
                ArtistName = "Artist 2",
                AlbumName = "Album 2",
                SharedByUsername = "User2",
                SharedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            user2Tracks.Add(track);
        }

        // Act: Filter tracks by user 1.
        var filteredUser1Tracks = await _trackRecordRepository.GetByUserAsync(user1Id, 0, 100);
        var filteredUser1List = filteredUser1Tracks.ToList();

        // Act: Filter tracks by user 2.
        var filteredUser2Tracks = await _trackRecordRepository.GetByUserAsync(user2Id, 0, 100);
        var filteredUser2List = filteredUser2Tracks.ToList();

        // Assert: User 1 filter should return only user 1's tracks.
        Assert.Equal(user1TrackCount, filteredUser1List.Count);
        Assert.All(filteredUser1List, track => Assert.Equal(user1Id, track.SharedByTelegramUserId));
        
        // Assert: All user 1 tracks should be in the filtered list.
        foreach (var expectedTrack in user1Tracks)
        {
            Assert.Contains(filteredUser1List, t => t.TrackRecordId == expectedTrack.TrackRecordId);
        }

        // Assert: User 2 filter should return only user 2's tracks.
        Assert.Equal(user2TrackCount, filteredUser2List.Count);
        Assert.All(filteredUser2List, track => Assert.Equal(user2Id, track.SharedByTelegramUserId));
        
        // Assert: All user 2 tracks should be in the filtered list.
        foreach (var expectedTrack in user2Tracks)
        {
            Assert.Contains(filteredUser2List, t => t.TrackRecordId == expectedTrack.TrackRecordId);
        }

        // Assert: No overlap between filtered lists.
        Assert.Empty(filteredUser1List.Intersect(filteredUser2List, new TrackRecordComparer()));
    }

    [Theory]
    [InlineData(1, 5)] // User shares 5 tracks, 2 are deleted.
    [InlineData(2, 10)] // User shares 10 tracks, 3 are deleted.
    public async Task UserFilter_ShouldExcludeDeletedTracks(long chatId, int totalTracks)
    {
        // Arrange: Create tracks, some deleted.
        var userId = 2001L;
        var deletedCount = totalTracks / 2;
        var activeTracks = new List<TrackRecordEntity>();

        for (int i = 0; i < totalTracks; i++)
        {
            var track = new TrackRecordEntity(chatId, $"track_{i}", userId)
            {
                TrackName = $"Track {i}",
                ArtistName = "Artist",
                AlbumName = "Album",
                SharedByUsername = "TestUser",
                IsDeleted = i < deletedCount // First half are deleted.
            };
            await _trackRecordRepository.CreateTrackRecordAsync(track);
            
            if (!track.IsDeleted)
            {
                activeTracks.Add(track);
            }
        }

        // Act: Filter tracks by user.
        var filteredTracks = await _trackRecordRepository.GetByUserAsync(userId, 0, 100);
        var filteredList = filteredTracks.ToList();

        // Assert: Should only return non-deleted tracks.
        Assert.Equal(totalTracks - deletedCount, filteredList.Count);
        Assert.All(filteredList, track => Assert.False(track.IsDeleted));
        
        // Assert: All active tracks should be in the filtered list.
        foreach (var expectedTrack in activeTracks)
        {
            Assert.Contains(filteredList, t => t.TrackRecordId == expectedTrack.TrackRecordId);
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
