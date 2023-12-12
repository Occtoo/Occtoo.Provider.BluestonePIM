using bluestone_inbound_formatter.Common;
using bluestone_inbound_formatter.Formatters;
using bluestone_inbound_formatter.Models;
using bluestone_inbound_formatter.Services;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bluestone_inbound_formatter.Features
{
    public class ProductOnboard
    {
        private readonly IOcctooService _occtooService;
        private readonly ITokenService _tokenService;
        private readonly IProductFormatter _productFormatter;
        private readonly IBlobService _blobService;
        private readonly ILogService _logService;

        public ProductOnboard(IOcctooService occtooService,
                              ITokenService tokenService,
                              IProductFormatter productFormatter,
                              IBlobService blobSerice,
                              ILogService logService)
        {
            _occtooService = occtooService;
            _tokenService = tokenService;
            _productFormatter = productFormatter;
            _blobService = blobSerice;
            _logService = logService;
        }

        [FunctionName("ProductOnboard")]
        public async Task Run([QueueTrigger("%ProductQueue%", Connection = "StorageConnectionString")] string myQueueItem)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
                var containterName = Environment.GetEnvironmentVariable("ProductContainer");

                var context = GetContext(myQueueItem);

                var blobContent = await _blobService.ReadJsonBlobAsync(myQueueItem, containterName, connectionString);
                await ParseAndImportFileProduct(blobContent, context);
            }
            catch (Exception ex)
            {
                LogMessageModel errorMessage = new(ex.Message, ex.StackTrace, true);
                await _logService.LogMessage(errorMessage, "ProductOnboard");
            }
        }

        private static string GetContext(string myQueueItem)
        {
            var defaultContext = Environment.GetEnvironmentVariable("DefaultContext");
            var additionalContexts = Environment.GetEnvironmentVariable("AdditionalContext");
            var contextDictionary = DictionaryHelper.ExtractToDictionary(defaultContext, additionalContexts);
            var contextString = myQueueItem.Split('_').FirstOrDefault().Trim();
            var context = contextDictionary.Where(x => x.Key == contextString).FirstOrDefault();
            return context.Value;
        }

        private async Task ParseAndImportFileProduct(string fileContent, string context)
        {
            var dataSource = Environment.GetEnvironmentVariable("ProductsDataSource");
            var dataSourceMedia = Environment.GetEnvironmentVariable("MediaDataSource");
            var dataProviderId = Environment.GetEnvironmentVariable("DataProviderId");
            var dataProviderSecret = Environment.GetEnvironmentVariable("DataProviderSecret");
            var productCodes = new List<string>();

            try
            {
                var productModel = JsonConvert.DeserializeObject<ProductResultModel>(fileContent);

                var dynamicEntities = await _productFormatter.FormatProducts(productModel.Results, context);
                var productEntities = dynamicEntities.Item1;
                var mediaEntities = dynamicEntities.Item2;
                productEntities = productEntities.GroupBy(entity => entity.Key).Select(group => group.First()).ToList();
                mediaEntities = mediaEntities.GroupBy(entity => entity.Key).Select(group => group.First()).ToList();

                var count = productEntities.Count;
                var json = JsonConvert.SerializeObject(productEntities);
                foreach (var batch in productEntities.Batch(50))
                {
                    foreach (var entity in batch)
                    {
                        productCodes.Add(entity.Key);
                    }

                    var token = await _tokenService.GetCachedToken(dataProviderId, dataProviderSecret, "OcctooToken");
                    await _occtooService.ImportEntitiesAsync(batch.ToList(), dataSource, dataProviderId, dataProviderSecret, token);
                }

                foreach (var batch in mediaEntities.Batch(50))
                {
                    var token = await _tokenService.GetCachedToken(dataProviderId, dataProviderSecret, "OcctooToken");
                    await _occtooService.ImportEntitiesAsync(batch.ToList(), dataSourceMedia, dataProviderId, dataProviderSecret, token);
                }
            }
            catch (Exception ex)
            {
                var json = JsonConvert.SerializeObject(productCodes);
                LogMessageModel errorMessage = new($"Products {json}: {ex.Message}", ex.StackTrace, true);
                await _logService.LogMessage(errorMessage, "ProductOnboard");
            }

        }

    }
}
