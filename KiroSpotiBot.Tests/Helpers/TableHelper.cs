using Azure.Data.Tables;

namespace KiroSpotiBot.Tests.Helpers;

/// <summary>
/// Helper class for managing Azure Table Storage operations in tests.
/// </summary>
public static class TableHelper
{
    /// <summary>
    /// Truncates (deletes all entities from) the specified table.
    /// </summary>
    /// <param name="tableClient">The table client for the table to truncate.</param>
    public static void TruncateTable(TableClient tableClient)
    {
        try
        {
            // Query all entities in the table.
            var entities = tableClient.Query<TableEntity>();
            
            // Delete each entity.
            foreach (var entity in entities)
            {
                tableClient.DeleteEntity(entity.PartitionKey, entity.RowKey);
            }
        }
        catch
        {
            // If table doesn't exist or other errors, ignore.
            // The repository will create it on first use.
        }
    }
    
    /// <summary>
    /// Truncates (deletes all entities from) the specified table by name.
    /// </summary>
    /// <param name="tableServiceClient">The table service client.</param>
    /// <param name="tableName">The name of the table to truncate.</param>
    public static void TruncateTable(TableServiceClient tableServiceClient, string tableName)
    {
        var tableClient = tableServiceClient.GetTableClient(tableName);
        TruncateTable(tableClient);
    }
}
