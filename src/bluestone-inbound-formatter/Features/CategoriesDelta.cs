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
    public class CategoriesDelta
    {
        private readonly IOcctooService _occtooService;
        private readonly ITokenService _tokenService;
        private readonly IBluestoneService _bluestoneService;
        private readonly ICategoryFormatter _categoryFormatter;
        private readonly ILogService _logService;

        public CategoriesDelta(IOcctooService occtooService,
                               ITokenService tokenService,
                               IBluestoneService bluestoneService,
                               ICategoryFormatter categoryFormatter,
                               ILogService logService)
        {
            _occtooService = occtooService;
            _tokenService = tokenService;
            _bluestoneService = bluestoneService;
            _categoryFormatter = categoryFormatter;
            _logService = logService;
        }


        [FunctionName("CategoriesDelta")]

        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            try
            {
                //var jsonWebhookData = req.Body;
                string jsonWebhookData = "{\"timestamp\":1701191512084,\"events\":[{\"changes\":{\"eventType\":\"PRODUCT_SYNC_DONE\",\"entityIds\":[],\"syncDoneData\":{\"field\":\"PAPI_SYNC_ID\",\"value\":\"65661f52f0a82700137d8d76\",\"context\":\"en\"}}}]}";
                var webHookModel = JsonConvert.DeserializeObject<WebhookModel>(jsonWebhookData);

                List<DifferenceModel> categoryDifferences = await GetCategoryDifferences(webHookModel);
                if (categoryDifferences != null && categoryDifferences.Any())
                {
                    var categoriesToAddUpdate = categoryDifferences.Where(x => x.DiffType != "DELETE").ToList();
                    var categoriesToDelete = categoryDifferences.Where(x => x.DiffType == "DELETE").ToList();

                    var categoryEntities = await GetCategoryEntities(categoriesToAddUpdate, categoriesToDelete, webHookModel);

                    if (categoryEntities != null && categoryEntities.Any())
                    {
                        await ImportEntitesAsync(categoryEntities);
                    }
                }

            }
            catch (Exception ex)
            {
                LogMessageModel errorMessage = new($"{ex.Message}", ex.StackTrace, true);
                await _logService.LogMessage(errorMessage, "CategoriesDelta");
            }
        }

        private async Task ImportEntitesAsync(List<DynamicEntity> categoryEntities)
        {
            var dataSource = Environment.GetEnvironmentVariable("CategoriesDataSource");
            var dataProviderId = Environment.GetEnvironmentVariable("DataProviderId");
            var dataProviderSecret = Environment.GetEnvironmentVariable("DataProviderSecret");

            foreach (var batch in categoryEntities.Batch(50))
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
