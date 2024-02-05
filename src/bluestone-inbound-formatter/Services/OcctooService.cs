using Occtoo.Onboarding.Sdk;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bluestone_inbound_provider.Services
{
    public interface IOcctooService
    {
        Task ImportEntitiesAsync(IReadOnlyList<DynamicEntity> entities, string dataSource, string dataProviderId, string dataProviderSecret, string token);
        Task<JsonDocument> GetTokenDocument(string dataProviderId, string dataProviderSecret);
    }

    public class OcctooService : IOcctooService
    {
        private readonly HttpClient _httpClient;

        public OcctooService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("OcctooBaseAddress"));

        }

        public async Task ImportEntitiesAsync(IReadOnlyList<DynamicEntity> entities, string dataSource, string dataProviderId, string dataProviderSecret, string token)
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderId, dataProviderSecret);
            var response = await onboardingServliceClient.StartEntityImportAsync(dataSource, entities, token);
            if (response.StatusCode != 202)
            {
                throw new Exception($"There was a problem with onboarding data to Occtoo Studio {response.StatusCode}");
            }
        }

        public async Task<JsonDocument> GetTokenDocument(string dataProviderId, string dataProviderSecret)
        {
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "dataProviders/tokens")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = dataProviderId,
                    secret = dataProviderSecret
                }), Encoding.UTF8, "application/json")
            };
            var tokenResponse = await _httpClient.SendAsync(tokenRequest);
            var tokenResponseContent = await tokenResponse.Content.ReadAsStreamAsync();
            var tokenDocument = JsonSerializer.Deserialize<JsonDocument>(tokenResponseContent);
            return tokenDocument;
        }

    }
}
