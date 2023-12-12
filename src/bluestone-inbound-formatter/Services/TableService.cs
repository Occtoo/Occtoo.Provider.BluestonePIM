using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace bluestone_inbound_formatter.Services
{
    public interface ITableService
    {
        Task<DynamicTableEntity> GetTableEntity(string tableName, string partitionKey, string rowKey);
        Task<string> GetDynamicTableEntity(string tableName, string partitionName, string rowKey, string value);
        Task AddDynamicTableEntity(string tableName, DynamicTableEntity entity);
    }
    public class TableService : ITableService
    {
        public async Task<DynamicTableEntity> GetTableEntity(string tableName, string partitionKey, string rowKey)
        {
            CloudTable table = await GetTableReference(tableName);

            TableOperation retrieveOperation = TableOperation.Retrieve<DynamicTableEntity>(partitionKey, rowKey);

            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            return retrievedResult.Result as DynamicTableEntity;

        }
        public async Task<string> GetDynamicTableEntity(string tableName, string partitionName, string rowKey, string value)
        {
            string date = string.Empty;
            var table = await GetTableReference(tableName);
            TableOperation getOperation = TableOperation.Retrieve(partitionName, rowKey);
            TableBatchOperation userEnvironmentBatch = new()
            {
                getOperation
            };
            var res = await table.ExecuteBatchAsync(userEnvironmentBatch);
            TableResult tableResult = res.FirstOrDefault();

            if (tableResult != null && tableResult.Result is DynamicTableEntity dynamicEntity)
            {
                if (dynamicEntity.Properties.TryGetValue(value, out EntityProperty property))
                {
                    if (property.PropertyType == Microsoft.WindowsAzure.Storage.Table.EdmType.String)
                    {
                        date = property.StringValue;
                    }
                }
            }

            return date;
        }
        public async Task AddDynamicTableEntity(string tableName, DynamicTableEntity entity)
        {
            var table = await GetTableReference(tableName);

            TableOperation addOperation = TableOperation.InsertOrReplace(entity);

            TableBatchOperation userEnvironmentBatch = new()
            {
                addOperation
            };

            await table.ExecuteBatchAsync(userEnvironmentBatch);
        }
        private static async Task<CloudTable> GetTableReference(string name)
        {
            var connection = Environment.GetEnvironmentVariable("StorageConnectionString");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connection);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference(name);

            await table.CreateIfNotExistsAsync();

            return table;
        }

    }
}
