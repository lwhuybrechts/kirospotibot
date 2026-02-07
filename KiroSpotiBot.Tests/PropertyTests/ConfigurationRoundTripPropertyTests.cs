using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Tests.Helpers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ExpectedObjects;

namespace KiroSpotiBot.Tests.PropertyTests;

/// <summary>
/// Property-based tests for configuration round-trip validation.
/// Property 5: Configuration Round-Trip
/// Validates: Requirements 3.2, 5.2, 9.4, 18.2
/// 
/// Note: These tests use xUnit's Theory attribute with InlineData to simulate
/// property-based testing behavior by testing multiple input combinations.
/// The GroupChats table is truncated before each test class instantiation to ensure clean state.
/// </summary>
public class ConfigurationRoundTripPropertyTests
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IGroupChatRepository _repository;
    private readonly TableClient _tableClient;

    public ConfigurationRoundTripPropertyTests()
    {
        // Use Azure Storage Emulator for testing.
        // Connection string for local development storage.
        var connectionString = "UseDevelopmentStorage=true";
        _tableServiceClient = new TableServiceClient(connectionString);
        
        var logger = Mock.Of<ILogger<BaseRepository<GroupChatEntity>>>();
        _repository = new GroupChatRepository(_tableServiceClient, logger);
        
        // Get reference to the GroupChats table.
        _tableClient = _tableServiceClient.GetTableClient("GroupChats");
        
        // Truncate the table before tests to ensure clean state.
        TableHelper.TruncateTable(_tableClient);
    }

    [Theory]
    [InlineData(12345, 67890, "37i9dQZF1DXcBWIGoYBM5M", "Today's Top Hits", 3)]
    [InlineData(11111, 22222, "5AB8PJLq8xCqXHJNqKJQzN", "RapCaviar", 5)]
    [InlineData(99999, 88888, "3cEYpjA9oz9GiPac4AsH4n", "Rock Classics", 10)]
    [InlineData(54321, 98765, null, null, 3)]
    [InlineData(77777, 66666, "7sZbq8QGyMnhKPcLJvCUFD", "Chill Vibes", 1)]
    [InlineData(10001, 20002, "37i9dQZF1DXcBWIGoYBM5M", null, 15)]
    [InlineData(30003, 40004, null, "Some Playlist", 20)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 5: Configuration Round-Trip")]
    public async Task ConfigurationRoundTrip_StoreAndRetrieve_ReturnsEquivalentValues(
        long telegramChatId,
        long administratorId,
        string? playlistId,
        string? playlistName,
        int downvoteThreshold)
    {
        // Arrange: Create configuration entity.
        var entity = new GroupChatEntity(telegramChatId, administratorId)
        {
            PlaylistId = playlistId,
            PlaylistName = playlistName,
            DownvoteThreshold = downvoteThreshold
        };
        
        // Act: Store the configuration.
        await _repository.CreateAsync(entity);
        
        // Retrieve the configuration.
        var retrieved = await _repository.GetByTelegramChatIdAsync(telegramChatId);
        
        // Assert: Verify all fields match using ExpectedObjects.
        Assert.NotNull(retrieved);
        
        new GroupChatEntity(telegramChatId, administratorId)
        {
            PlaylistId = playlistId,
            PlaylistName = playlistName,
            DownvoteThreshold = downvoteThreshold
        }.ToExpectedObject(config => config
            .Ignore(x => x.CreatedAt)
            .Ignore(x => x.Timestamp)
            .Ignore(x => x.ETag)
        ).ShouldEqual(retrieved);
    }

    [Theory]
    [InlineData(12345, 67890, "37i9dQZF1DXcBWIGoYBM5M", "Initial Playlist", 3, "5AB8PJLq8xCqXHJNqKJQzN", "Updated Playlist", 5)]
    [InlineData(11111, 22222, "5AB8PJLq8xCqXHJNqKJQzN", "RapCaviar", 5, "3cEYpjA9oz9GiPac4AsH4n", "Rock Classics", 10)]
    [InlineData(99999, 88888, null, null, 3, "7sZbq8QGyMnhKPcLJvCUFD", "New Playlist", 7)]
    [InlineData(54321, 98765, "37i9dQZF1DXcBWIGoYBM5M", "Old Name", 3, "37i9dQZF1DXcBWIGoYBM5M", "New Name", 3)]
    [InlineData(77777, 66666, "7sZbq8QGyMnhKPcLJvCUFD", "Chill Vibes", 1, null, null, 15)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 5: Configuration Round-Trip")]
    public async Task ConfigurationUpdate_ModifyAndRetrieve_ReturnsUpdatedValues(
        long telegramChatId,
        long administratorId,
        string? initialPlaylistId,
        string? initialPlaylistName,
        int initialThreshold,
        string? updatedPlaylistId,
        string? updatedPlaylistName,
        int updatedThreshold)
    {
        // Arrange: Create initial configuration.
        var entity = new GroupChatEntity(telegramChatId, administratorId)
        {
            PlaylistId = initialPlaylistId,
            PlaylistName = initialPlaylistName,
            DownvoteThreshold = initialThreshold
        };
        
        await _repository.CreateAsync(entity);
        
        // Act: Update the configuration.
        var retrieved = await _repository.GetByTelegramChatIdAsync(telegramChatId);
        Assert.NotNull(retrieved);
        
        retrieved.PlaylistId = updatedPlaylistId;
        retrieved.PlaylistName = updatedPlaylistName;
        retrieved.DownvoteThreshold = updatedThreshold;
        
        await _repository.UpdateAsync(retrieved);
        
        // Retrieve again.
        var final = await _repository.GetByTelegramChatIdAsync(telegramChatId);
        
        // Assert: Verify updated fields match using ExpectedObjects.
        Assert.NotNull(final);
        
        new GroupChatEntity(telegramChatId, administratorId)
        {
            PlaylistId = updatedPlaylistId,
            PlaylistName = updatedPlaylistName,
            DownvoteThreshold = updatedThreshold
        }.ToExpectedObject(config => config
            .Ignore(x => x.CreatedAt)
            .Ignore(x => x.Timestamp)
            .Ignore(x => x.ETag)
        ).ShouldEqual(final);
    }

    [Theory]
    [InlineData(1, 12345, 67890)]
    [InlineData(3, 11111, 22222)]
    [InlineData(5, 99999, 88888)]
    [InlineData(10, 54321, 98765)]
    [InlineData(15, 77777, 66666)]
    [InlineData(20, 10001, 20002)]
    [InlineData(50, 30003, 40004)]
    [InlineData(100, 50005, 60006)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 5: Configuration Round-Trip")]
    public async Task DownvoteThreshold_StorePositiveInteger_RetrievesCorrectValue(
        int threshold,
        long telegramChatId,
        long administratorId)
    {
        // Arrange.
        var entity = new GroupChatEntity(telegramChatId, administratorId)
        {
            DownvoteThreshold = threshold
        };
        
        // Act.
        await _repository.CreateAsync(entity);
        var retrieved = await _repository.GetByTelegramChatIdAsync(telegramChatId);
        
        // Assert.
        Assert.NotNull(retrieved);
        Assert.Equal(threshold, retrieved.DownvoteThreshold);
    }

    [Theory]
    [InlineData(12345, 67890, "37i9dQZF1DXcBWIGoYBM5M", "Today's Top Hits", 3)]
    [InlineData(11111, 22222, "5AB8PJLq8xCqXHJNqKJQzN", "RapCaviar", 5)]
    [InlineData(99999, 88888, null, null, 10)]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 5: Configuration Round-Trip")]
    public async Task ConfigurationRoundTrip_MultipleOperations_MaintainsDataIntegrity(
        long telegramChatId,
        long administratorId,
        string? playlistId,
        string? playlistName,
        int downvoteThreshold)
    {
        // Arrange & Act: Create.
        var entity = new GroupChatEntity(telegramChatId, administratorId)
        {
            PlaylistId = playlistId,
            PlaylistName = playlistName,
            DownvoteThreshold = downvoteThreshold
        };
        
        await _repository.CreateAsync(entity);
        var afterCreate = await _repository.GetByTelegramChatIdAsync(telegramChatId);
        
        // Act: Update multiple times.
        Assert.NotNull(afterCreate);
        afterCreate.DownvoteThreshold = downvoteThreshold + 1;
        await _repository.UpdateAsync(afterCreate);
        
        var afterFirstUpdate = await _repository.GetByTelegramChatIdAsync(telegramChatId);
        Assert.NotNull(afterFirstUpdate);
        afterFirstUpdate.PlaylistName = playlistName + " Updated";
        await _repository.UpdateAsync(afterFirstUpdate);
        
        // Final retrieval.
        var final = await _repository.GetByTelegramChatIdAsync(telegramChatId);
        
        // Assert: Verify final state using ExpectedObjects.
        Assert.NotNull(final);
        
        new GroupChatEntity(telegramChatId, administratorId)
        {
            PlaylistId = playlistId,
            PlaylistName = playlistName + " Updated",
            DownvoteThreshold = downvoteThreshold + 1
        }.ToExpectedObject(config => config
            .Ignore(x => x.CreatedAt)
            .Ignore(x => x.Timestamp)
            .Ignore(x => x.ETag)
        ).ShouldEqual(final);
    }

    [Theory]
    [InlineData(12345, 67890, "37i9dQZF1DXcBWIGoYBM5M")]
    [InlineData(11111, 22222, "5AB8PJLq8xCqXHJNqKJQzN")]
    [InlineData(99999, 88888, "3cEYpjA9oz9GiPac4AsH4n")]
    [Trait("Feature", "telegram-spotify-bot")]
    [Trait("Property", "Property 5: Configuration Round-Trip")]
    public async Task ConfigurationRoundTrip_DefaultValues_ArePreserved(
        long telegramChatId,
        long administratorId,
        string playlistId)
    {
        // Arrange: Create entity with only required fields (default threshold = 3).
        var entity = new GroupChatEntity(telegramChatId, administratorId)
        {
            PlaylistId = playlistId
        };
        
        // Act.
        await _repository.CreateAsync(entity);
        var retrieved = await _repository.GetByTelegramChatIdAsync(telegramChatId);
        
        // Assert: Verify default threshold is preserved.
        Assert.NotNull(retrieved);
        Assert.Equal(3, retrieved.DownvoteThreshold); // Default value.
        Assert.Equal(playlistId, retrieved.PlaylistId);
        Assert.NotEqual(default(DateTime), retrieved.CreatedAt);
    }
}
