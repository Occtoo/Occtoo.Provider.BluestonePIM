using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace bluestone_inbound_formatter.Services
{
    public interface ITokenService
    {
        Task<string> GetToken(string dataProviderId, string dataProviderSecret);

        Task<string> GetCachedToken(string dataProviderId, string dataProviderSecret, string tokenName);
        Task SaveToken(string token, string tokenName);
    }
    public class TokenService : ITokenService
    {
        private readonly IOcctooService _occtooService;
        private readonly ITableService _tableService;
        public TokenService(IOcctooService occtooService, ITableService tableService)
        { 
            _occtooService= occtooService;
            _tableService= tableService;
        }

        public async Task<string> GetToken(string dataProviderId, string dataProviderSecret)
        {
            var tokenDocument = await _occtooService.GetTokenDocument(dataProviderId, dataProviderSecret);
            var token = tokenDocument.RootElement.GetProperty("result").GetProperty("accessToken").GetString();
            return token;
        }

        public async Task<string> GetCachedToken(string dataProviderId, string dataProviderSecret, string tokenName)
        {
            string token = string.Empty;
            var entity = await _tableService.GetTableEntity("OcctooToken", "Token", tokenName);
            if (entity != null && entity.Properties.ContainsKey("OcctooTokenValue"))
            {
                string inputTime = entity.Timestamp.ToString();
                DateTime parsedTime = DateTime.Parse(inputTime);

                if (IsWithinOneHour(parsedTime))
                {
                    token = entity.Properties["OcctooTokenValue"].StringValue;
                }
                else
                {
                    var tokenDocument = await _occtooService.GetTokenDocument(dataProviderId, dataProviderSecret);
                    token = tokenDocument.RootElement.GetProperty("result").GetProperty("accessToken").GetString();

                    await SaveToken(token, tokenName);

                }
            }
            if (string.IsNullOrEmpty(token))
            {
                var tokenDocument = await _occtooService.GetTokenDocument(dataProviderId, dataProviderSecret);
                token = tokenDocument.RootElement.GetProperty("result").GetProperty("accessToken").GetString();

                await SaveToken(token, tokenName);

            }
            return token;

        }

        public async Task SaveToken(string token, string tokenName)
        {
            if (token != null)
            {
                DynamicTableEntity entity = new("Token", tokenName);
                entity.Properties["OcctooTokenValue"] = new EntityProperty(token);
                await _tableService.AddDynamicTableEntity("OcctooToken", entity);
            }
        }

        private static bool IsWithinOneHour(DateTime timeToCheck)
        {
            TimeSpan difference = DateTime.Now - timeToCheck;
            return difference.TotalMinutes >= 0 && difference.TotalMinutes <= 59;
        }

    }
}
