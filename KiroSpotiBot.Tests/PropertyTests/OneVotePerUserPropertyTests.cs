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
/// Property-based tests for one vote per user per track constraint.
/// Property 22: One Vote Per User Per Track
/// Validates: Requirements 12.5
/// </summary>
public class OneVotePerUserPropertyTests
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

    public OneVotePerUserPropertyTests()
    {
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var voteLogger = Mock.Of<ILogger<BaseRepository<VoteEntity>>>();
        _voteRepository = new VoteRepository(_tableServiceClient, voteLogger);
        
        var trackRecordLogger = Mock.Of<ILogger<BaseRepository<TrackRecordEntity>>>();
        _trackRecordRepository = new TrackRecordRepository(_tableServiceClient, trackRecordLogger);
        
        var groupChatLogger = Mock.Of<ILogger<BaseRepository<GroupChatEntity>>>();
        _groupChatRepository = new GroupChatRepository(_tableServiceClient, groupChatLogger);
        
        var userLogger = Mock.Of<ILogger<BaseRepository<UserEntity>>>();
        _userRepository = new UserRepository(_tableServiceClient, Mock.Of<IEncryptionService>(), userLogger);
        
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
    [InlineData(12345, 67890, "track123")]
    [InlineData(11111, 22222, "track456")]
    [InlineData(99999, 88888, "track789")]
    [InlineData(54321, 98765, "track101")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 22: One Vote Per User Per Track")]
    public async Task OneVotePerUser_MultipleVoteAttempts_OnlyOneVoteExists(
        long telegramChatId,
        long telegramUserId,
        string trackSpotifyId)
    {
        // Arrange: Create test data.
        await SetupTestDataAsync(telegramChatId, telegramUserId, trackSpotifyId);
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        // Act: Record multiple votes from same user.
        await _voteManager.RecordVoteAsync(
            trackRecord.TrackRecordId, telegramChatId, telegramUserId, "Upvote", "user", null);
        
        await _voteManager.RecordVoteAsync(
            trackRecord.TrackRecordId, telegramChatId, telegramUserId, "Upvote", "user", null);
        
        await _voteManager.RecordVoteAsync(
            trackRecord.TrackRecordId, telegramChatId, telegramUserId, "Downvote", "user", null);
        
        // Assert: Only one vote should exist.
        var votes = await _voteRepository.GetByTrackRecordAsync(trackRecord.TrackRecordId);
        Assert.Single(votes);
        Assert.Equal("Downvote", votes.First().VoteType);
    }

    [Theory]
    [InlineData(12345, 67890, "track123", 5)]
    [InlineData(11111, 22222, "track456", 10)]
    [InlineData(99999, 88888, "track789", 3)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 22: One Vote Per User Per Track")]
    public async Task OneVotePerUser_MultipleUsers_EachHasOneVote(
        long telegramChatId,
        long baseUserId,
        string trackSpotifyId,
        int userCount)
    {
        // Arrange: Create test data.
        await SetupTestDataAsync(telegramChatId, baseUserId, trackSpotifyId);
        var trackRecord = (await _trackRecordRepository.GetByGroupChatAsync(telegramChatId)).First();
        
        // Act: Record votes from multiple users.
        for (int i = 0; i < userCount; i++)
        {
            await _voteManager.RecordVoteAsync(
                trackRecord.TrackRecordId,
                telegramChatId,
                baseUserId + i,
                i % 2 == 0 ? "Upvote" : "Downvote",
                $"user{i}",
                null);
        }
        
        // Assert: Each user should have exactly one vote.
        var votes = await _voteRepository.GetByTrackRecordAsync(trackRecord.TrackRecordId);
        Assert.Equal(userCount, votes.Count());
        
        // Verify each user has only one vote.
        var userIds = votes.Select(v => v.TelegramUserId).ToList();
        Assert.Equal(userCount, userIds.Distinct().Count());
    }

    private async Task SetupTestDataAsync(long telegramChatId, long telegramUserId, string trackSpotifyId)
    {
        var groupChat = new GroupChatEntity(telegramChatId, telegramUserId)
        {
            PlaylistId = "playlist123",
            DownvoteThreshold = 10
        };
        await _groupChatRepository.CreateAsync(groupChat);
        
        var user = new UserEntity(telegramUserId);
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
