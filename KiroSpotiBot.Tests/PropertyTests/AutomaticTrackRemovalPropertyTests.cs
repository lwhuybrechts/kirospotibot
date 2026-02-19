using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Infrastructure.Services;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for automatic track removal at threshold.
/// Property 26: Automatic Track Removal at Threshold
/// Validates: Requirements 13.1, 13.3, 13.5
/// </summary>
public class AutomaticTrackRemovalPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IVoteRepository _voteRepository;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly IGroupChatRepository _groupChatRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVoteManager _voteManager;
    private readonly Mock<ISpotifyService> _spotifyServiceMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly TableClient _votesTableClient;
    private readonly TableClient _trackRecordsTableClient;
    private readonly TableClient _groupChatsTableClient;
    private readonly TableClient _usersTableClient;

    public AutomaticTrackRemovalPropertyTests()
    {
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var voteLogger = Mock.Of<ILogger<BaseRepository<VoteEntity>>>();
        _voteRepository = new VoteRepository(_tableServiceClient, voteLogger);
        
        var trackRecordLogger = Mock.Of<ILogger<BaseRepository<TrackRecordEntity>>>();
        _trackRecordRepository = new TrackRecordRepository(_tableServiceClient, trackRecordLogger);
        
        var groupChatLogger = Mock.Of<ILogger<BaseRepository<GroupChatEntity>>>();
        _groupChatRepository = new GroupChatRepository(_tableServiceClient, groupChatLogger);
        
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _encryptionServiceMock.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _encryptionServiceMock.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        
        var userLogger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        _userRepository = new UserRepository(_tableServiceClient, _encryptionServiceMock.Object, userLogger);
        
        _spotifyServiceMock = new Mock<ISpotifyService>();
        _spotifyServiceMock.Setup(s => s.RemoveTrackFromPlaylistAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var voteManagerLogger = Mock.Of<ILogger<VoteManager>>();
        _voteManager = new VoteManager(
            _voteRepository,
            _trackRecordRepository,
            _groupChatRepository,
            _spotifyServiceMock.Object,
            _userRepository,
            voteManagerLogger);
        
        _votesTableClient = _tableServiceClient.GetTableClient("Votes");
        _trackRecordsTableClient = _tableServiceClient.GetTableClient("TrackRecords");
        _groupChatsTableClient = _tableServiceClient.GetTableClient("GroupChats");
        _usersTableClient = _tableServiceClient.GetTableClient("Users");
        
        TableHelper.TruncateTable(_votesTableClient);
        TableHelper.TruncateTable(_trackRecordsTableClient);
        TableHelper.TruncateTable(_groupChatsTableClient);
        TableHelper.TruncateTable(_usersTableClient);
    }

    [Theory]
    [InlineData(12345, 67890, "track123", 3)]
    [InlineData(11111, 22222, "track456", 5)]
    [InlineData(99999, 88888, "track789", 1)]
    [InlineData(54321, 98765, "track101", 10)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 26: Automatic Track Removal at Threshold")]
    public async Task AutomaticRemoval_ReachesThreshold_RemovesTrackAndMarksDeleted(
        long telegramChatId,
        long baseUserId,
        string trackSpotifyId,
        int threshold)
    {
        // Arrange: Create test data with specific threshold.
        await SetupTestDataAsync(telegramChatId, baseUserId, trackSpotifyId, threshold);
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        // Act: Add downvotes up to threshold.
        for (int i = 0; i < threshold; i++)
        {
            await _voteManager.RecordVoteAsync(
                trackRecord.TrackRecordId,
                telegramChatId,
                baseUserId + i,
                "Downvote",
                $"user{i}",
                null);
        }
        
        // Assert: Track should be marked as deleted.
        var updatedTrackRecord = await _trackRecordRepository.GetByIdAsync(trackRecord.TrackRecordId, telegramChatId);
        Assert.NotNull(updatedTrackRecord);
        Assert.True(updatedTrackRecord.IsDeleted, $"Track should be deleted after {threshold} downvotes");
        Assert.Equal(threshold, updatedTrackRecord.DownvoteCount);
        
        // Verify Spotify API was called to remove track.
        _spotifyServiceMock.Verify(s => s.RemoveTrackFromPlaylistAsync(
            It.IsAny<string>(),
            trackSpotifyId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(12345, 67890, "track123", 3, 2)]
    [InlineData(11111, 22222, "track456", 5, 4)]
    [InlineData(99999, 88888, "track789", 10, 9)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 26: Automatic Track Removal at Threshold")]
    public async Task AutomaticRemoval_BelowThreshold_DoesNotRemoveTrack(
        long telegramChatId,
        long baseUserId,
        string trackSpotifyId,
        int threshold,
        int downvoteCount)
    {
        // Arrange.
        await SetupTestDataAsync(telegramChatId, baseUserId, trackSpotifyId, threshold);
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        // Act: Add downvotes below threshold.
        bool trackRemoved = false;
        for (int i = 0; i < downvoteCount; i++)
        {
            trackRemoved = await _voteManager.RecordVoteAsync(
                trackRecord.TrackRecordId,
                telegramChatId,
                baseUserId + i,
                "Downvote",
                $"user{i}",
                null);
        }
        
        // Assert: Track should not be removed.
        Assert.False(trackRemoved);
        
        var updatedTrackRecord = await _trackRecordRepository.GetByIdAsync(trackRecord.TrackRecordId, telegramChatId);
        Assert.NotNull(updatedTrackRecord);
        Assert.False(updatedTrackRecord.IsDeleted);
        
        // Verify Spotify API was not called.
        _spotifyServiceMock.Verify(s => s.RemoveTrackFromPlaylistAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(12345, 67890, "track123", 3, 5, 10)]
    [InlineData(11111, 22222, "track456", 5, 3, 8)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 26: Automatic Track Removal at Threshold")]
    public async Task AutomaticRemoval_CountsAbsoluteDownvotes_IgnoresUpvotes(
        long telegramChatId,
        long baseUserId,
        string trackSpotifyId,
        int threshold,
        int upvoteCount,
        int downvoteCount)
    {
        // Arrange.
        await SetupTestDataAsync(telegramChatId, baseUserId, trackSpotifyId, threshold);
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        // Act: Add upvotes.
        for (int i = 0; i < upvoteCount; i++)
        {
            await _voteManager.RecordVoteAsync(
                trackRecord.TrackRecordId,
                telegramChatId,
                baseUserId + i,
                "Upvote",
                $"user{i}",
                null);
        }
        
        // Add downvotes until threshold is reached (track will be deleted at threshold).
        for (int i = 0; i < downvoteCount; i++)
        {
            await _voteManager.RecordVoteAsync(
                trackRecord.TrackRecordId,
                telegramChatId,
                baseUserId + upvoteCount + i,
                "Downvote",
                $"user{upvoteCount + i}",
                null);
        }
        
        // Assert: Track should be removed when downvote count reaches threshold.
        // Note: Downvote count will be at threshold, not total downvoteCount, because voting stops after deletion.
        var updatedTrackRecord = await _trackRecordRepository.GetByIdAsync(trackRecord.TrackRecordId, telegramChatId);
        Assert.NotNull(updatedTrackRecord);
        Assert.True(updatedTrackRecord.IsDeleted, "Track should be deleted when downvote count reaches threshold");
        Assert.Equal(threshold, updatedTrackRecord.DownvoteCount);
        Assert.Equal(upvoteCount, updatedTrackRecord.UpvoteCount);
    }

    private async Task SetupTestDataAsync(long telegramChatId, long telegramUserId, string trackSpotifyId, int threshold)
    {
        var groupChat = new GroupChatEntity(telegramChatId, telegramUserId)
        {
            PlaylistId = "playlist123",
            DownvoteThreshold = threshold
        };
        await _groupChatRepository.CreateAsync(groupChat);
        
        var user = new UserEntity(telegramUserId)
        {
            EncryptedAccessToken = "test_access_token",
            EncryptedRefreshToken = "test_refresh_token"
        };
        await _userRepository.CreateAsync(user);
        
        var trackRecord = new TrackRecordEntity(telegramChatId, trackSpotifyId, telegramUserId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            AlbumName = "Test Album",
            TelegramMessageId = 12345
        };
        await _trackRecordRepository.CreateAsync(trackRecord);
    }
}
