using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace bluestone_inbound_provider.Services
{
    public interface IBlobService
    {
        Task<string> ReadJsonBlobAsync(string blobName, string containerName, string connectionString);
        Task UploadJsonBlobAsync(string fileName, string json, string containerName, string connectionString);
        Task SendToQueue(string fileName, string queueName, string connectionString);
        Task ArchiveImportedBlobAsync(string blobName, string containerName, string connectionString);
    }
    public class BlobService : IBlobService
    {
        public async Task<string> ReadJsonBlobAsync(string blobName, string containerName, string connectionString)
        {

            BlobServiceClient blobServiceClient = new(Environment.GetEnvironmentVariable("StorageConnectionString"));
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");
            }

            BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();

            using StreamReader reader = new(blobDownloadInfo.Content);
            string json = await reader.ReadToEndAsync();
            return json;
        }

        public async Task ArchiveImportedBlobAsync(string blobName, string containerName, string connectionString)
        {

            BlobServiceClient blobServiceClient = new(Environment.GetEnvironmentVariable("StorageConnectionString"));
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return;
            }
         
            var archive = containerClient.GetBlobClient($"archive/{DateTime.Now.ToString("yyyy-MM-dd")}/{blobName}");

            CopyFromUriOperation operation = await archive.StartCopyFromUriAsync(blobClient.Uri);
            await operation.WaitForCompletionAsync();

            await blobClient.DeleteIfExistsAsync();
        }

        public async Task UploadJsonBlobAsync(string fileName, string json, string containerName, string connectionString)
        {
            BlobServiceClient blobServiceClient = new(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream);
        }

        public async Task SendToQueue(string fileName, string queueName, string connectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync();

            CloudQueueMessage message = new(fileName);
            await queue.AddMessageAsync(message);

        }

    }
}
