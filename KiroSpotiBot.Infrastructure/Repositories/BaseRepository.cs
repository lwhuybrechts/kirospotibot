using Azure;
using Azure.Data.Tables;
using KiroSpotiBot.Core.Entities;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Base repository implementation for Azure Table Storage with retry logic.
/// </summary>
/// <typeparam name="T">Entity type that inherits from MyTableEntity</typeparam>
public class BaseRepository<T> : IRepository<T> where T : MyTableEntity, new()
{
    private readonly TableClient _tableClient;
    private readonly ILogger<BaseRepository<T>> _logger;
    private readonly string _tableName;

    public BaseRepository(TableServiceClient tableServiceClient, string tableName, ILogger<BaseRepository<T>> logger)
    {
        _tableName = tableName;
        _logger = logger;
        _tableClient = tableServiceClient.GetTableClient(tableName);
        
        // Ensure table exists
        _tableClient.CreateIfNotExists();
    }

    public async Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity from {TableName} with PartitionKey={PartitionKey}, RowKey={RowKey}", 
                _tableName, partitionKey, rowKey);
            throw;
        }
    }

    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.AddEntityAsync(entity, cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity in {TableName}", _tableName);
            throw;
        }
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity in {TableName}", _tableName);
            throw;
        }
    }

    public async Task<T> UpsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting entity in {TableName}", _tableName);
            throw;
        }
    }

    public async Task DeleteAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Entity doesn't exist, ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity from {TableName} with PartitionKey={PartitionKey}, RowKey={RowKey}", 
                _tableName, partitionKey, rowKey);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync(string filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<T>();
            await foreach (var entity in _tableClient.QueryAsync<T>(filter, cancellationToken: cancellationToken))
            {
                results.Add(entity);
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying entities from {TableName} with filter={Filter}", _tableName, filter);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetByPartitionKeyAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        return await QueryAsync($"PartitionKey eq '{partitionKey}'", cancellationToken);
    }
}
