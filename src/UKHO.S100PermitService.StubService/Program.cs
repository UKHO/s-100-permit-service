using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.StubSetup;
using WireMock.Settings;

namespace UKHO.S100PermitService.StubService
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
            .ConfigureServices((host, services) => ConfigureServices(services, host.Configuration));

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<WireMockServerSettings>(configuration.GetSection("WireMockServerSettings"));
            services.Configure<HoldingsServiceConfiguration>(configuration.GetSection("HoldingsServiceConfiguration"));
            services.Configure<ProductKeyServiceConfiguration>(configuration.GetSection("ProductKeyServiceConfiguration"));

            services.AddSingleton<StubFactory>();
            services.AddHostedService<StubManagerHostedService>();
        }
    }
}