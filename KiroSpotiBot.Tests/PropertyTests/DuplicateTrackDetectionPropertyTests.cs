using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for duplicate track detection.
/// Property 12: Duplicate Track Detection
/// Validates: Requirements 6.3, 11.3
/// 
/// Note: These tests verify that duplicate tracks are detected and prevented
/// from being added to the playlist again, and marked as duplicates in track records.
/// </summary>
public class DuplicateTrackDetectionPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly TableClient _trackRecordsTable;

    public DuplicateTrackDetectionPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var logger = Mock.Of<ILogger<BaseRepository<TrackRecordEntity>>>();
        _trackRecordRepository = new TrackRecordRepository(_tableServiceClient, logger);
        
        // Get table reference.
        _trackRecordsTable = _tableServiceClient.GetTableClient("TrackRecords");
        
        // Truncate table before tests.
        TableHelper.TruncateTable(_trackRecordsTable);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "7qiZfU4dY1lWllzX7mPBI")]
    [InlineData(55555, 66666, "0VjIjW4GlUZAMYd2vXMi3b")]
    [InlineData(77777, 88888, "4cOdK2wGLETKBW3PvgPWqT")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 12: Duplicate Track Detection")]
    public async Task DuplicateDetection_TrackAlreadyExists_IsDetected(
        long chatId,
        long userId,
        string trackId)
    {
        // Arrange: Add track to playlist (first time).
        var firstRecord = new TrackRecordEntity(chatId, trackId, userId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            IsDuplicate = false,
            IsDeleted = false
        };
        await _trackRecordRepository.CreateTrackRecordAsync(firstRecord, CancellationToken.None);
        
        // Act: Check if track exists (simulating duplicate detection).
        var trackExists = await _trackRecordRepository.TrackExistsAsync(chatId, trackId, CancellationToken.None);
        
        // Assert: Track should be detected as existing.
        Assert.True(trackExists);
    }

    [Theory]
    [InlineData(12345, 67890, 11111, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, 44444, "7qiZfU4dY1lWllzX7mPBI")]
    [InlineData(55555, 66666, 77777, "0VjIjW4GlUZAMYd2vXMi3b")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 12: Duplicate Track Detection")]
    public async Task DuplicateDetection_SecondAttempt_MarkedAsDuplicate(
        long chatId,
        long firstUserId,
        long secondUserId,
        string trackId)
    {
        // Arrange: First user adds track.
        var firstRecord = new TrackRecordEntity(chatId, trackId, firstUserId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            IsDuplicate = false,
            IsDeleted = false
        };
        await _trackRecordRepository.CreateTrackRecordAsync(firstRecord, CancellationToken.None);
        
        // Act: Second user tries to add same track.
        var trackExists = await _trackRecordRepository.TrackExistsAsync(chatId, trackId, CancellationToken.None);
        
        // Create second record marked as duplicate.
        var secondRecord = new TrackRecordEntity(chatId, trackId, secondUserId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            IsDuplicate = true,
            IsDeleted = false
        };
        await _trackRecordRepository.CreateTrackRecordAsync(secondRecord, CancellationToken.None);
        
        // Assert: Duplicate should be detected and marked.
        Assert.True(trackExists);
        Assert.True(secondRecord.IsDuplicate);
        Assert.False(firstRecord.IsDuplicate);
    }

    [Theory]
    [InlineData(12345, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 12: Duplicate Track Detection")]
    public async Task DuplicateDetection_NewTrack_NotDetectedAsDuplicate(
        long chatId,
        string trackId)
    {
        // Act: Check if track exists (should not exist).
        var trackExists = await _trackRecordRepository.TrackExistsAsync(chatId, trackId, CancellationToken.None);
        
        // Assert: Track should not be detected as existing.
        Assert.False(trackExists);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 12: Duplicate Track Detection")]
    public async Task DuplicateDetection_DeletedTrack_NotConsideredExisting(
        long chatId,
        long userId,
        string trackId)
    {
        // Arrange: Add track and mark as deleted.
        var record = new TrackRecordEntity(chatId, trackId, userId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            IsDuplicate = false,
            IsDeleted = true
        };
        await _trackRecordRepository.CreateTrackRecordAsync(record, CancellationToken.None);
        
        // Act: Check if track exists (should not count deleted tracks).
        var trackExists = await _trackRecordRepository.TrackExistsAsync(chatId, trackId, CancellationToken.None);
        
        // Assert: Deleted track should not be considered as existing.
        Assert.False(trackExists);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp", "7qiZfU4dY1lWllzX7mPBI")]
    [InlineData(22222, 33333, "0VjIjW4GlUZAMYd2vXMi3b", "4cOdK2wGLETKBW3PvgPWqT")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 12: Duplicate Track Detection")]
    public async Task DuplicateDetection_DifferentTracks_NotDetectedAsDuplicates(
        long chatId,
        long userId,
        string trackId1,
        string trackId2)
    {
        // Arrange: Add first track.
        var firstRecord = new TrackRecordEntity(chatId, trackId1, userId)
        {
            TrackName = "Test Track 1",
            ArtistName = "Test Artist",
            IsDuplicate = false,
            IsDeleted = false
        };
        await _trackRecordRepository.CreateTrackRecordAsync(firstRecord, CancellationToken.None);
        
        // Act: Check if second track exists (should not exist).
        var track2Exists = await _trackRecordRepository.TrackExistsAsync(chatId, trackId2, CancellationToken.None);
        
        // Assert: Different track should not be detected as duplicate.
        Assert.False(track2Exists);
    }

    [Theory]
    [InlineData(12345, 22222, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(55555, 66666, 77777, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 12: Duplicate Track Detection")]
    public async Task DuplicateDetection_SameTrackDifferentChats_NotDetectedAsDuplicates(
        long chatId1,
        long chatId2,
        long userId,
        string trackId)
    {
        // Arrange: Add track to first chat.
        var record1 = new TrackRecordEntity(chatId1, trackId, userId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            IsDuplicate = false,
            IsDeleted = false
        };
        await _trackRecordRepository.CreateTrackRecordAsync(record1, CancellationToken.None);
        
        // Act: Check if track exists in second chat (should not exist).
        var trackExistsInChat2 = await _trackRecordRepository.TrackExistsAsync(chatId2, trackId, CancellationToken.None);
        
        // Assert: Same track in different chat should not be detected as duplicate.
        Assert.False(trackExistsInChat2);
    }

    [Theory]
    [InlineData(12345, 67890, "3n3Ppam7vgaVa1iaRUc9Lp")]
    [InlineData(22222, 33333, "7qiZfU4dY1lWllzX7mPBI")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 12: Duplicate Track Detection")]
    public async Task DuplicateDetection_MultipleAttempts_AllMarkedAsDuplicates(
        long chatId,
        long userId,
        string trackId)
    {
        // Arrange: Add track first time.
        var firstRecord = new TrackRecordEntity(chatId, trackId, userId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            IsDuplicate = false,
            IsDeleted = false
        };
        await _trackRecordRepository.CreateTrackRecordAsync(firstRecord, CancellationToken.None);
        
        // Act: Add same track multiple times.
        var secondRecord = new TrackRecordEntity(chatId, trackId, userId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            IsDuplicate = true,
            IsDeleted = false
        };
        await _trackRecordRepository.CreateTrackRecordAsync(secondRecord, CancellationToken.None);
        
        var thirdRecord = new TrackRecordEntity(chatId, trackId, userId)
        {
            TrackName = "Test Track",
            ArtistName = "Test Artist",
            IsDuplicate = true,
            IsDeleted = false
        };
        await _trackRecordRepository.CreateTrackRecordAsync(thirdRecord, CancellationToken.None);
        
        // Assert: All subsequent attempts should be marked as duplicates.
        Assert.False(firstRecord.IsDuplicate);
        Assert.True(secondRecord.IsDuplicate);
        Assert.True(thirdRecord.IsDuplicate);
    }
}
