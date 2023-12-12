using bluestone_inbound_formatter.Common;
using bluestone_inbound_formatter.Formatters;
using bluestone_inbound_formatter.Models;
using bluestone_inbound_formatter.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bluestone_inbound_formatter.Features
{
    public class ProductDelta
    {
        private readonly IOcctooService _occtooService;
        private readonly ITokenService _tokenService;
        private readonly IBluestoneService _bluestoneService;
        private readonly IProductFormatter _productFormatter;
        private readonly ILogService _logService;

        public ProductDelta(IOcctooService occtooService,
                               ITokenService tokenService,
                               IBluestoneService bluestoneService,
                               IProductFormatter productFormatter,
                               ILogService logService)
        {
            _occtooService = occtooService;
            _tokenService = tokenService;
            _bluestoneService = bluestoneService;
            _productFormatter = productFormatter;
            _logService = logService;
        }
        [FunctionName("ProductDelta")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            string jsonWebhookData = "{\"timestamp\":1701789437382,\"events\":[{\"changes\":{\"eventType\":\"PRODUCT_SYNC_DONE\",\"entityIds\":[],\"syncDoneData\":{\"field\":\"PAPI_SYNC_ID\",\"value\":\"656f3ef6e2f4d70012a166ba\",\"context\":\"en\"}}}]}";
            try
            {
                //var jsonWebhookData = req.Body;
                var webHookModel = JsonConvert.DeserializeObject<WebhookModel>(jsonWebhookData);
                List<DifferenceModel> productDifferences = await GetProductDifferences(webHookModel);
                if (productDifferences != null && productDifferences.Any())
                {
                    var productsToAddUpdate = productDifferences.Where(x => x.DiffType != "DELETE").ToList();
                    var productsToDelete = productDifferences.Where(x => x.DiffType == "DELETE").ToList();

                    var productMediaEntities = await GetProductEntities(productsToAddUpdate, productsToDelete, webHookModel);

                    var productEntities = productMediaEntities.Item1;
                    var mediaEntities = productMediaEntities.Item2;

                    if (productEntities != null && productEntities.Any())
                    {
                        var dataSource = Environment.GetEnvironmentVariable("ProductsDataSource");
                        await ImportEntitesAsync(productEntities, dataSource);
                    }

                    if (mediaEntities != null && mediaEntities.Any())
                    {
                        var dataSource = Environment.GetEnvironmentVariable("MediaDataSource");
                        await ImportEntitesAsync(mediaEntities, dataSource);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessageModel errorMessage = new($"{ex.Message}", ex.StackTrace, true);
                await _logService.LogMessage(errorMessage, "ProductsDelta");

            }

        }


        private async Task ImportEntitesAsync(List<DynamicEntity> entities, string dataSource)
        {            
            var dataProviderId = Environment.GetEnvironmentVariable("DataProviderId");
            var dataProviderSecret = Environment.GetEnvironmentVariable("DataProviderSecret");

            foreach (var batch in entities.Batch(50))
            {
                var token = await _tokenService.GetCachedToken(dataProviderId, dataProviderSecret, "OcctooToken");

                await _occtooService.ImportEntitiesAsync(batch.ToList(), dataSource, dataProviderId, dataProviderSecret, token);
            }

        }

        private async Task<(List<DynamicEntity>, List<DynamicEntity>)> GetProductEntities(List<DifferenceModel> productsToAddUpdate, List<DifferenceModel> productsToDelete, WebhookModel webHookModel)
        {
            List<DynamicEntity> productEntities = new();
            List<DynamicEntity> mediaEntities = new();
            var defaultContext = Environment.GetEnvironmentVariable("DefaultContext");
            var additionalContexts = Environment.GetEnvironmentVariable("AdditionalContext");
            var contextDictionary = DictionaryHelper.ExtractToDictionary(defaultContext, additionalContexts);

            foreach (var webEvent in webHookModel.Events)
            {
                var context = webEvent.Changes.SyncDoneData.Context;
                if (context != null && !string.IsNullOrEmpty(contextDictionary[context]))
                {
                    var contextKeyValue = contextDictionary.Where(x => x.Key == context).FirstOrDefault();
                    List<ProductModel> products = await GetProductsFromBluestone(productsToAddUpdate, contextKeyValue);
                    var productMediaEntities = await _productFormatter.FormatProducts(products, contextKeyValue.Value);
                    productEntities = productMediaEntities.Item1;
                    mediaEntities = productMediaEntities.Item2;
                    var productEntitesDelete = await _productFormatter.FormatProductsToDelete(productsToDelete);
                    productEntities.AddRange(productEntitesDelete);
                }
            }

            return (productEntities, mediaEntities);
        }

        private async Task<List<ProductModel>> GetProductsFromBluestone(List<DifferenceModel> productsToAddUpdate, KeyValuePair<string, string> context)
        {
            List<ProductModel> products = new();
            foreach (var productDiff in productsToAddUpdate)
            {
                var product = await GetProductById(productDiff.Id);
                if (product != null)
                {
                    products.Add(product);
                }
            }

            return products;
        }

        private async Task<ProductModel> GetProductById(string id)
        {
            var product = await _bluestoneService.GetProductById(id);
            return product;
        }

        private async Task<List<DifferenceModel>> GetProductDifferences(WebhookModel webhookModel)
        {
            List<DifferenceModel> productDifferences = new();

            foreach (var webHookEvent in webhookModel.Events)
            {
                var syncId = webHookEvent.Changes.SyncDoneData.Value.ToString();
                string baseUrlTemplate = Environment.GetEnvironmentVariable("BluestoneDifferencesProduct");
                var productDiffs = await _bluestoneService.GetDifferences(syncId, baseUrlTemplate);
                productDifferences.AddRange(productDiffs);
            }

            return productDifferences;
        }

    }
}
