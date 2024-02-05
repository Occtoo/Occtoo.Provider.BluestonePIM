using bluestone_inbound_provider.Common;
using bluestone_inbound_provider.Formatting;
using bluestone_inbound_provider.Models;
using bluestone_inbound_provider.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace bluestone_inbound_provider.Features
{
    public class CategoriesFullSync
    {
        private readonly IOcctooService _occtooService;
        private readonly ITokenService _tokenService;
        private readonly IBluestoneService _bluestoneService;
        private readonly ICategoryFormatter _categoryFormatter;
        private readonly ILogService _logService;

        public CategoriesFullSync(IOcctooService occtooService,
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

        [FunctionName("CategoriesFullSync")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            var dataSource = Environment.GetEnvironmentVariable("CategoriesDataSource");
            var dataProviderId = Environment.GetEnvironmentVariable("DataProviderId");
            var dataProviderSecret = Environment.GetEnvironmentVariable("DataProviderSecret");

            var defaultContext = Environment.GetEnvironmentVariable("DefaultContext");
            var additionalContexts = Environment.GetEnvironmentVariable("AdditionalContext");
            var contextDictionary = DictionaryHelper.ExtractToDictionary(defaultContext, additionalContexts);
            try
            {
                foreach (var context in contextDictionary)
                {
                    var categoriesModel = await _bluestoneService.GetAllCategories(context.Key);
                    var categoryEntities = _categoryFormatter.FormatCategories(categoriesModel.Results.ToList(), context);

                    if (categoryEntities != null && categoryEntities.Any())
                    {
                        foreach (var batch in categoryEntities.Batch(50))
                        {
                            var token = await _tokenService.GetCachedToken(dataProviderId, dataProviderSecret, "OcctooToken");
                            await _occtooService.ImportEntitiesAsync(batch.ToList(), dataSource, dataProviderId, dataProviderSecret, token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessageModel errorMessage = new($"{ex.Message}", ex.StackTrace, true);
                await _logService.LogMessage(errorMessage, "CategoriesFullSync");

            }

        }


    }
}
