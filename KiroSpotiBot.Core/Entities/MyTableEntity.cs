using Azure;
using Azure.Data.Tables;

namespace KiroSpotiBot.Core.Entities;

/// <summary>
/// Base class for all Azure Table Storage entities.
/// Provides common ITableEntity properties.
/// </summary>
public abstract class MyTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
