using bluestone_inbound_provider;
using bluestone_inbound_provider.Formatting;
using bluestone_inbound_provider.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(Startup))]
namespace bluestone_inbound_provider
{
    internal class Startup :FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            ConfigureServices(builder.Services);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IOcctooService, OcctooService>();
            services.AddSingleton<IOcctooMediaService, OcctooMediaService>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddTransient<IBluestoneService, BluestoneService>();
            services.AddTransient<IBlobService, BlobService>();
            services.AddTransient<ITableService, TableService>();
            services.AddTransient<ILogService, LogService>();
            services.AddTransient<ICategoryFormatter, CategoryFormatter>();
            services.AddTransient<IProductFormatter, ProductFormatter>();


            services.AddHttpClient<IOcctooService, OcctooService>((client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            }))
            .AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient<IOcctooMediaService, OcctooMediaService>((client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            }))
           .AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient<ITokenService, TokenService>((client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            }))
               .AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient<IBluestoneService, BluestoneService>((client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            }))
               .AddPolicyHandler(GetRetryPolicy());

        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

    }
}
