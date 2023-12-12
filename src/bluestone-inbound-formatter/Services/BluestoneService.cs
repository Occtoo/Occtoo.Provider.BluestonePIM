using bluestone_inbound_formatter.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace bluestone_inbound_formatter.Services
{
    public interface IBluestoneService
    {
        Task<ProductResultModel> GetAllProducts(string cursor, int limit, string context);
        Task<ProductModel> GetProductById(string id);
        Task<CategoriesModel> GetAllCategories(string context);
        Task<CategoryModel> GetCategoryById(string id, KeyValuePair<string, string> context);
        Task<List<SyncModel>> GetSyncs(long createdAfter);
        Task<List<DifferenceModel>> GetDifferences(string id, string baseUrlTemplate);
    }
    public  class BluestoneService : IBluestoneService
    {
        private readonly HttpClient _httpClient;

        public BluestoneService (HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("BluestoneAPI"));
            _httpClient.DefaultRequestHeaders.Add(Environment.GetEnvironmentVariable("BluestoneAPIKeyName"),
                                      Environment.GetEnvironmentVariable("BluestoneAPIKeyValue"));


        }

        public async Task<ProductResultModel> GetAllProducts(string cursor, int limit, string context)
        {

            if (_httpClient.DefaultRequestHeaders.Contains("context"))
            {
                _httpClient.DefaultRequestHeaders.Remove("context");
            }
            _httpClient.DefaultRequestHeaders.Add("context", context);

            var payload = new
            {
                cursor,
                limit,
            };
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
            HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(Environment.GetEnvironmentVariable("BluestoneProductsCursor"), content);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ProductResultModel>(jsonResponse);
            }
            return null;
        }

        public async Task<ProductModel> GetProductById(string id)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{Environment.GetEnvironmentVariable("BluestoneProducts")}{id}");
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var product = JsonConvert.DeserializeObject<ProductModel>(jsonResponse);
                return product;
            }
            return null;

        }
        public async Task<CategoriesModel> GetAllCategories(string context)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("context"))
            {
                _httpClient.DefaultRequestHeaders.Remove("context");
            }
            _httpClient.DefaultRequestHeaders.Add("context", context);

            HttpResponseMessage response = await _httpClient.GetAsync(Environment.GetEnvironmentVariable("BluestoneCategoriesScan"));
           
            if (response.IsSuccessStatusCode)
            {
                StringBuilder sb = new();
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    {
                        string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        sb.Append(chunk);
                    }
                }

                var categories = JsonConvert.DeserializeObject<CategoriesModel>(sb.ToString());
                return categories;
            }

            return null;
        }

        public async Task<CategoryModel> GetCategoryById(string id, KeyValuePair<string, string> context)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("context"))
            {
                _httpClient.DefaultRequestHeaders.Remove("context");
            }
            _httpClient.DefaultRequestHeaders.Add("context", context.Key);

            HttpResponseMessage response = await _httpClient.GetAsync($"{Environment.GetEnvironmentVariable("BluestoneCategories")}{id}");
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var category = JsonConvert.DeserializeObject<CategoryModel>(jsonResponse);
                return category;
            }
            return null;
        }

        public async Task<List<DifferenceModel>> GetDifferences(string id, string baseUrlTemplate)
        {
            string url = string.Format(baseUrlTemplate, id);

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                StringBuilder sb = new ();
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    {
                        string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        sb.Append(chunk);
                    }
                }

                var difference = JsonConvert.DeserializeObject<List<DifferenceModel>>(sb.ToString());
                return difference;
            }

            return null;
        }

        public async Task<List<SyncModel>> GetSyncs(long createdAfter)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{Environment.GetEnvironmentVariable("BluestoneSync")}?createdAfter={createdAfter}");
            if (response.IsSuccessStatusCode)
            {
                StringBuilder sb = new();
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    {
                        string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        sb.Append(chunk);
                    }
                }

                var syncs = JsonConvert.DeserializeObject<List<SyncModel>>(sb.ToString());
                return syncs;
            }

            return null;
        }
    }
}
