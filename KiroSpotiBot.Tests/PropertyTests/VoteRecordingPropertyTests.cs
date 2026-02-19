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
/// Property-based tests for vote recording and updates.
/// Property 21: Vote Recording and Updates
/// Validates: Requirements 12.3, 12.4, 12.6
/// 
/// Note: These tests use xUnit's Theory attribute with InlineData to simulate
/// property-based testing behavior by testing multiple input combinations.
/// </summary>
public class VoteRecordingPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IVoteRepository _voteRepository;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly IGroupChatRepository _groupChatRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVoteManager _voteManager;
    private readonly TableClient _votesTableClient;
    private readonly TableClient _trackRecordsTableClient;
    private readonly TableClient _groupChatsTableClient;
    private readonly TableClient _usersTableClient;

    public VoteRecordingPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        // Create repositories.
        var voteLogger = Mock.Of<ILogger<BaseRepository<VoteEntity>>>();
        _voteRepository = new VoteRepository(_tableServiceClient, voteLogger);
        
        var trackRecordLogger = Mock.Of<ILogger<BaseRepository<TrackRecordEntity>>>();
        _trackRecordRepository = new TrackRecordRepository(_tableServiceClient, trackRecordLogger);
        
        var groupChatLogger = Mock.Of<ILogger<BaseRepository<GroupChatEntity>>>();
        _groupChatRepository = new GroupChatRepository(_tableServiceClient, groupChatLogger);
        
        var userLogger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        _userRepository = new UserRepository(_tableServiceClient, Mock.Of<IEncryptionService>(), userLogger);
        
        // Create VoteManager with mocked Spotify service.
        var spotifyService = new Mock<ISpotifyService>();
        spotifyService.Setup(s => s.RemoveTrackFromPlaylistAsync(
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
            spotifyService.Object,
            _userRepository,
            voteManagerLogger);
        
        // Get table clients and truncate.
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
    [InlineData(12345, 67890, "track123", "Upvote", "user1", "https://avatar1.com")]
    [InlineData(11111, 22222, "track456", "Downvote", "user2", "https://avatar2.com")]
    [InlineData(99999, 88888, "track789", "Upvote", "user3", null)]
    [InlineData(54321, 98765, "track101", "Downvote", "user4", "https://avatar4.com")]
    [InlineData(77777, 66666, "track202", "Upvote", "user5", null)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 21: Vote Recording and Updates")]
    public async Task VoteRecording_NewVote_CreatesVoteRecord(
        long telegramChatId,
        long telegramUserId,
        string trackSpotifyId,
        string voteType,
        string username,
        string? avatarUrl)
    {
        // Arrange: Create group chat, user, and track record.
        await SetupTestDataAsync(telegramChatId, telegramUserId, trackSpotifyId);
        
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        // Act: Record a vote.
        await _voteManager.RecordVoteAsync(
            trackRecord.TrackRecordId,
            telegramChatId,
            telegramUserId,
            voteType,
            username,
            avatarUrl);
        
        // Assert: Verify vote was created.
        var vote = await _voteRepository.GetVoteAsync(trackRecord.TrackRecordId, telegramUserId);
        Assert.NotNull(vote);
        Assert.Equal(voteType, vote.VoteType);
        Assert.Equal(username, vote.VoterUsername);
        Assert.Equal(avatarUrl, vote.VoterAvatarUrl);
    }

    [Theory]
    [InlineData(12345, 67890, "track123", "Upvote", "Downvote")]
    [InlineData(11111, 22222, "track456", "Downvote", "Upvote")]
    [InlineData(99999, 88888, "track789", "Upvote", "Downvote")]
    [InlineData(54321, 98765, "track101", "Downvote", "Upvote")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 21: Vote Recording and Updates")]
    public async Task VoteRecording_ChangeVote_UpdatesVoteType(
        long telegramChatId,
        long telegramUserId,
        string trackSpotifyId,
        string initialVoteType,
        string updatedVoteType)
    {
        // Arrange: Create test data and initial vote.
        await SetupTestDataAsync(telegramChatId, telegramUserId, trackSpotifyId);
        
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        await _voteManager.RecordVoteAsync(
            trackRecord.TrackRecordId,
            telegramChatId,
            telegramUserId,
            initialVoteType,
            "testuser",
            null);
        
        // Act: Change the vote.
        await _voteManager.RecordVoteAsync(
            trackRecord.TrackRecordId,
            telegramChatId,
            telegramUserId,
            updatedVoteType,
            "testuser",
            null);
        
        // Assert: Verify vote was updated.
        var vote = await _voteRepository.GetVoteAsync(trackRecord.TrackRecordId, telegramUserId);
        Assert.NotNull(vote);
        Assert.Equal(updatedVoteType, vote.VoteType);
        
        // Verify only one vote exists for this user.
        var allVotes = await _voteRepository.GetByTrackRecordAsync(trackRecord.TrackRecordId);
        Assert.Single(allVotes);
    }

    [Theory]
    [InlineData(12345, 67890, "track123", 2, 1)]
    [InlineData(11111, 22222, "track456", 5, 3)]
    [InlineData(99999, 88888, "track789", 10, 0)]
    [InlineData(54321, 98765, "track101", 0, 7)]
    [InlineData(77777, 66666, "track202", 3, 3)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 21: Vote Recording and Updates")]
    public async Task VoteRecording_MultipleVotes_UpdatesDenormalizedCounts(
        long telegramChatId,
        long baseUserId,
        string trackSpotifyId,
        int upvoteCount,
        int downvoteCount)
    {
        // Arrange: Create test data.
        await SetupTestDataAsync(telegramChatId, baseUserId, trackSpotifyId);
        
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        // Act: Record multiple upvotes.
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
        
        // Record multiple downvotes.
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
        
        // Assert: Verify denormalized counts in track record.
        var updatedTrackRecord = await _trackRecordRepository.GetByIdAsync(trackRecord.TrackRecordId, telegramChatId);
        Assert.NotNull(updatedTrackRecord);
        Assert.Equal(upvoteCount, updatedTrackRecord.UpvoteCount);
        Assert.Equal(downvoteCount, updatedTrackRecord.DownvoteCount);
    }

    [Theory]
    [InlineData(12345, 67890, "track123")]
    [InlineData(11111, 22222, "track456")]
    [InlineData(99999, 88888, "track789")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 21: Vote Recording and Updates")]
    public async Task VoteRecording_RemoveVote_UpdatesDenormalizedCounts(
        long telegramChatId,
        long telegramUserId,
        string trackSpotifyId)
    {
        // Arrange: Create test data and vote.
        await SetupTestDataAsync(telegramChatId, telegramUserId, trackSpotifyId);
        
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        await _voteManager.RecordVoteAsync(
            trackRecord.TrackRecordId,
            telegramChatId,
            telegramUserId,
            "Upvote",
            "testuser",
            null);
        
        // Act: Remove the vote.
        await _voteManager.RemoveVoteAsync(
            trackRecord.TrackRecordId,
            telegramChatId,
            telegramUserId);
        
        // Assert: Verify vote was removed.
        var vote = await _voteRepository.GetVoteAsync(trackRecord.TrackRecordId, telegramUserId);
        Assert.Null(vote);
        
        // Verify denormalized counts updated.
        var updatedTrackRecord = await _trackRecordRepository.GetByIdAsync(trackRecord.TrackRecordId, telegramChatId);
        Assert.NotNull(updatedTrackRecord);
        Assert.Equal(0, updatedTrackRecord.UpvoteCount);
        Assert.Equal(0, updatedTrackRecord.DownvoteCount);
    }

    private async Task SetupTestDataAsync(long telegramChatId, long telegramUserId, string trackSpotifyId)
    {
        // Create group chat.
        var groupChat = new GroupChatEntity(telegramChatId, telegramUserId)
        {
            PlaylistId = "playlist123",
            DownvoteThreshold = 10 // High threshold to prevent auto-removal in tests.
        };
        await _groupChatRepository.CreateAsync(groupChat);
        
        // Create user.
        var user = new UserEntity(telegramUserId);
        await _userRepository.CreateAsync(user);
        
        // Create track record.
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
