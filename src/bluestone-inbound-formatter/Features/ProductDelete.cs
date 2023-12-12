using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using bluestone_inbound_formatter.Formatters;
using bluestone_inbound_formatter.Services;
using bluestone_inbound_formatter.Models;
using System.Collections.Generic;
using Occtoo.Onboarding.Sdk.Models;
using bluestone_inbound_formatter.Common;
using System.Linq;

namespace bluestone_inbound_formatter.Features
{
    public class ProductDelete
    {
        private readonly IOcctooService _occtooService;
        private readonly ITokenService _tokenService;
        private readonly IBluestoneService _bluestoneService;
        private readonly IProductFormatter _productFormatter;
        private readonly ILogService _logService;

        public ProductDelete(IOcctooService occtooService,
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

        [FunctionName("ProductDelete")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            //var jsonWebhookData = req.Body;
            string jsonWebhookData = "{\"timestamp\":1701856990167,\"events\":[{\"changes\":{\"eventType\":\"PRODUCT_WATCH_STATE\",\"entityIds\":[\"655dcad0cb2efc2b3bcd183b\"],\"stateChange\":{\"changeType\":\"UPDATE\",\"oldValue\":\"CONNECTED\",\"newValue\":\"TO_BE_ARCHIVED\",\"context\":\"en\"}}}]}";
            try
            {
                var webHookModel = JsonConvert.DeserializeObject<WebhookModel>(jsonWebhookData);
                var productIds = GetProductId(webHookModel);
                var products = new List<ProductModel>();
                foreach (var productId in productIds)
                {
                    var product = await GetProductsFromBluestone(productId);
                    products.Add(product);
                }
                var entities = FormatProductsToDelete(products);
                if (entities != null && entities.Any())
                {
                    var dataSource = Environment.GetEnvironmentVariable("ProductsDataSource");
                    await ImportEntitesAsync(entities, dataSource);
                }
            }
            catch (Exception ex)
            {
                LogMessageModel errorMessage = new($"{ex.Message}", ex.StackTrace, true);
                await _logService.LogMessage(errorMessage, "ProductDelete");

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

        private List<DynamicEntity> FormatProductsToDelete(List<ProductModel> products)
        {
            var entities = _productFormatter.FormatProductsToDelete(products);
            return entities;
        }

        private async Task<ProductModel> GetProductsFromBluestone(string productId)
        {
            var product = await _bluestoneService.GetProductById(productId);
            return product;
        }

        private static List<string> GetProductId(WebhookModel webhookModel)
        {
            var productIdList = new List<string>();
            foreach (var webHookEvent in webhookModel.Events)
            {
                if (webHookEvent.Changes.StateChange.NewValue == "ARCHIVED")
                {
                    foreach (var entityId in webHookEvent.Changes.EntityIds)
                    {
                        var productId = entityId.ToString();
                        productIdList.Add(productId);
                    }
                }
            }

            return productIdList;
        }

    }
}
