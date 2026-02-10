using Azure;
using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using KiroSpotiBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Repository for OAuth state operations.
/// </summary>
public class OAuthStateRepository : IOAuthStateRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<OAuthStateRepository> _logger;
    private const string TableName = "OAuthStates";

    public OAuthStateRepository(TableServiceClient tableServiceClient, ILogger<OAuthStateRepository> logger)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
        _tableClient.CreateIfNotExists();
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<OAuthStateEntity> CreateAsync(OAuthStateEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.AddEntityAsync(entity, cancellationToken);
            _logger.LogInformation("Created OAuth state {State} for user {TelegramUserId}.", entity.State, entity.TelegramUserId);
            return entity;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to create OAuth state {State}.", entity.State);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<OAuthStateEntity?> GetByStateAsync(string state, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<OAuthStateEntity>("OAUTHSTATE", state, cancellationToken: cancellationToken);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("OAuth state {State} not found.", state);
            return null;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to retrieve OAuth state {State}.", state);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string state, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.DeleteEntityAsync("OAUTHSTATE", state, cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted OAuth state {State}.", state);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("OAuth state {State} not found for deletion.", state);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete OAuth state {State}.", state);
            throw;
        }
    }
}
