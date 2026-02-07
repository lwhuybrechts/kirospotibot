using KiroSpotiBot.Core.Entities;

namespace KiroSpotiBot.Infrastructure.Repositories;

/// <summary>
/// Base repository interface for Azure Table Storage operations.
/// </summary>
/// <typeparam name="T">Entity type that inherits from MyTableEntity</typeparam>
public interface IRepository<T> where T : MyTableEntity, new()
{
    Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpsertAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> QueryAsync(string filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetByPartitionKeyAsync(string partitionKey, CancellationToken cancellationToken = default);
}
