using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Functions;

/// <summary>
/// Azure Function for cleaning up expired OAuth state records.
/// Runs every hour to remove states older than their expiration time.
/// </summary>
public class OAuthStateCleanupFunction
{
    private readonly ILogger<OAuthStateCleanupFunction> _logger;
    private readonly TableServiceClient _tableServiceClient;
    private const string TableName = "OAuthStates";

    public OAuthStateCleanupFunction(
        ILogger<OAuthStateCleanupFunction> logger,
        TableServiceClient tableServiceClient)
    {
        _logger = logger;
        _tableServiceClient = tableServiceClient;
    }

    /// <summary>
    /// Runs weekly to clean up expired OAuth states.
    /// NCRONTAB format: {second} {minute} {hour} {day} {month} {day-of-week}
    /// "0 0 2 * * 0" = At 2:00 AM every Sunday.
    /// </summary>
    [Function("OAuthStateCleanup")]
    public async Task Run([TimerTrigger("0 0 2 * * 0")] TimerInfo timerInfo)
    {
        _logger.LogInformation("Starting OAuth state cleanup job at {Time}.", DateTime.UtcNow);

        try
        {
            var tableClient = _tableServiceClient.GetTableClient(TableName);
            var now = DateTime.UtcNow;
            var deletedCount = 0;

            // Query all OAuth states.
            var query = tableClient.QueryAsync<OAuthStateEntity>(
                filter: $"PartitionKey eq 'OAUTHSTATE'");

            // Delete expired states.
            await foreach (var state in query)
            {
                if (state.ExpiresAt < now)
                {
                    try
                    {
                        await tableClient.DeleteEntityAsync(state.PartitionKey, state.RowKey);
                        deletedCount++;
                        _logger.LogDebug(
                            "Deleted expired OAuth state {State} for user {UserId}. Expired at {ExpiresAt}.",
                            state.State,
                            state.TelegramUserId,
                            state.ExpiresAt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to delete expired OAuth state {State}.",
                            state.State);
                    }
                }
            }

            _logger.LogInformation(
                "OAuth state cleanup completed. Deleted {DeletedCount} expired states.",
                deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth state cleanup.");
        }
    }
}
