using bluestone_inbound_provider.Common;
using bluestone_inbound_provider.Formatting;
using bluestone_inbound_provider.Models;
using bluestone_inbound_provider.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace bluestone_inbound_provider.Features
{
    public class DataSync
    {
        private readonly IOcctooService _occtooService;
        private readonly ITokenService _tokenService;
        private readonly IBluestoneService _bluestoneService;
        private readonly IProductFormatter _productFormatter;
        private readonly ICategoryFormatter _categoryFormatter;
        private readonly ILogService _logService;

        public DataSync(IOcctooService occtooService,
                               ITokenService tokenService,
                               IBluestoneService bluestoneService,
                                IProductFormatter productFormatter,
                               ICategoryFormatter categoryFormatter,
                               ILogService logService)
        {
            _occtooService = occtooService;
            _tokenService = tokenService;
            _bluestoneService = bluestoneService;
            _productFormatter = productFormatter;
            _categoryFormatter = categoryFormatter;
            _logService = logService;
        }

        [FunctionName("DataSync")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            try
            {
                string jsonWebhookData = await new StreamReader(req.Body).ReadToEndAsync();
                var webHookModel = JsonConvert.DeserializeObject<WebhookModel>(jsonWebhookData);

                await ProcessCategoryDeltas(webHookModel);
                await ProcessProductDeltas(webHookModel);

            }
            catch (Exception ex)
            {
                LogMessageModel errorMessage = new($"{ex.Message}", ex.StackTrace, true);
                await _logService.LogMessage(errorMessage, "DataSync");
            }
        }

        private async Task ProcessProductDeltas(WebhookModel webHookModel)
        {
            List<DifferenceModel> productDifferences = await GetProductDifferences(webHookModel);
            if (productDifferences != null && productDifferences.Any())
            {
                var productsToAddUpdate = productDifferences.Where(x => x.DiffType != "DELETE").ToList();

                var productMediaEntities = await GetProductEntities(productsToAddUpdate, webHookModel);

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

        private async Task<(List<DynamicEntity>, List<DynamicEntity>)> GetProductEntities(List<DifferenceModel> productsToAddUpdate, WebhookModel webHookModel)
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

        private async Task ProcessCategoryDeltas(WebhookModel webHookModel)
        {
            List<DifferenceModel> categoryDifferences = await GetCategoryDifferences(webHookModel);
            if (categoryDifferences != null && categoryDifferences.Any())
            {
                var categoriesToAddUpdate = categoryDifferences.Where(x => x.DiffType != "DELETE").ToList();
                var categoriesToDelete = categoryDifferences.Where(x => x.DiffType == "DELETE").ToList();

                var categoryEntities = await GetCategoryEntities(categoriesToAddUpdate, categoriesToDelete, webHookModel);

                if (categoryEntities != null && categoryEntities.Any())
                {
                    await ImportEntitesAsync(categoryEntities, Environment.GetEnvironmentVariable("CategoriesDataSource"));
                }
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

        private async Task<List<DynamicEntity>> GetCategoryEntities(List<DifferenceModel> categoriesToAddUpdate, List<DifferenceModel> categoriesToDelete, WebhookModel webhookModel)
        {
            List<DynamicEntity> categoryEntities = new();
            var defaultContext = Environment.GetEnvironmentVariable("DefaultContext");
            var additionalContexts = Environment.GetEnvironmentVariable("AdditionalContext");
            var contextDictionary = DictionaryHelper.ExtractToDictionary(defaultContext, additionalContexts);

            foreach (var webEvent in webhookModel.Events)
            {
                var context = webEvent.Changes.SyncDoneData.Context;
                if (context != null && !string.IsNullOrEmpty(contextDictionary[context]))
                {
                    var contextKeyValue = contextDictionary.Where(x => x.Key == context).FirstOrDefault();
                    List<CategoryModel> categories = await GetCategoriesFromBluestone(categoriesToAddUpdate, contextKeyValue);
                    categoryEntities = _categoryFormatter.FormatCategories(categories, contextKeyValue);
                    var categoryEntitesDelete = _categoryFormatter.FormatCategoriesDelete(categoriesToDelete);
                    categoryEntities.AddRange(categoryEntitesDelete);
                }
            }
            return categoryEntities;
        }

        private async Task<List<DifferenceModel>> GetCategoryDifferences(WebhookModel webhookModel)
        {
            List<DifferenceModel> categoryDifferences = new();

            foreach (var webHookEvent in webhookModel.Events)
            {
                var syncId = webHookEvent.Changes.SyncDoneData.Value.ToString();
                string baseUrlTemplate = Environment.GetEnvironmentVariable("BluestoneDifferencesCategory");
                var categoryDiffs = await _bluestoneService.GetDifferences(syncId, baseUrlTemplate);
                categoryDifferences.AddRange(categoryDiffs);
            }

            return categoryDifferences;
        }

        private async Task<List<CategoryModel>> GetCategoriesFromBluestone(List<DifferenceModel> categoriesToAddUpdate, KeyValuePair<string, string> context)
        {
            List<CategoryModel> categories = new();
            foreach (var categoryDiff in categoriesToAddUpdate)
            {
                var category = await GetCategoryById(categoryDiff.Id, context);
                if (category != null)
                {
                    categories.Add(category);
                }
            }

            return categories;
        }

        private async Task<CategoryModel> GetCategoryById(string categoryId, KeyValuePair<string, string> context)
        {
            var category = await _bluestoneService.GetCategoryById(categoryId, context);
            return category;
        }

    }
}
