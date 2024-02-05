using bluestone_inbound_provider.Common;
using bluestone_inbound_provider.Models;
using bluestone_inbound_provider.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace bluestone_inbound_provider.Features
{
    public class ProductFullSync
    {
        private readonly IBluestoneService _bluestoneService;
        private readonly IBlobService _blobService;
        private readonly ILogService _logService;

        public ProductFullSync(IBluestoneService bluestoneService,
                               IBlobService blobSerice,
                               ILogService logService)
        {
            _bluestoneService = bluestoneService;
            _blobService = blobSerice;
            _logService = logService;
        }

        [FunctionName("ProductFullSync")]
        public async Task Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            try
            {
                var defaultContext = Environment.GetEnvironmentVariable("DefaultContext");
                var additionalContexts = Environment.GetEnvironmentVariable("AdditionalContext");
                var contextDictionary = DictionaryHelper.ExtractToDictionary(defaultContext, additionalContexts);
                foreach (var context in contextDictionary)
                {
                    await SendProductsToBlob(context.Key);
                }
            }
            catch (Exception ex) 
            {
                LogMessageModel errorMessage = new($"{ex.Message}", ex.StackTrace, true);
                await _logService.LogMessage(errorMessage, "ProductsFullSync");

            }
        }

        private async Task SendProductsToBlob(string context)
        {
            var cursor = "string";
            var productLimit = Environment.GetEnvironmentVariable("ProductsLimit");
            int limit = int.Parse(productLimit);

            var productResult = await GetProducts(cursor, limit, context);
            while (productResult != null && productResult.Results.Any())
            {
                string fileName = $"{context}_{cursor}_{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}.json";
                var json = JsonConvert.SerializeObject(productResult);

                await UploadJsonBlobAsync(fileName, json);
                await SendToQueue(fileName);

                cursor = productResult.NextCursor;
                productResult = await GetProducts(cursor, limit, context);

            }
        }

        private async Task<ProductResultModel> GetProducts(string cursor, int limit, string context)
        {
            return await _bluestoneService.GetAllProducts(cursor, limit, context);
        }

        private async Task UploadJsonBlobAsync(string fileName, string json)
        {
            string connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            string containerName = Environment.GetEnvironmentVariable("ProductContainer");
            await _blobService.UploadJsonBlobAsync(fileName, json, containerName, connectionString); 
        }

        private async Task SendToQueue(string fileName)
        {
            string connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            string queueName = Environment.GetEnvironmentVariable("ProductQueue");
            await _blobService.SendToQueue(fileName, queueName, connectionString);
        }


    }
}
