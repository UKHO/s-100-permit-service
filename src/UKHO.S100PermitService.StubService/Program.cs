using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.StubSetup;
using WireMock.Server;
using WireMock.Settings;

namespace UKHO.S100PermitService.StubService
{
    internal static class Program
    {
        private static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            services.Configure<WireMockServerSettings>(configuration.GetSection("WireMockServerSettings"));
            services.Configure<HoldingsServiceConfiguration>(configuration.GetSection("HoldingsServiceConfiguration"));
            services.Configure<ProductKeyServiceConfiguration>(configuration.GetSection("ProductKeyServiceConfiguration"));

            var serviceProvider = services.BuildServiceProvider();

            var wireMockServerSettings = serviceProvider.GetService<IOptions<WireMockServerSettings>>()?.Value;
            var holdingsServiceConfiguration = serviceProvider.GetService<IOptions<HoldingsServiceConfiguration>>()?.Value;
            var productKeyServiceConfiguration = serviceProvider.GetService<IOptions<ProductKeyServiceConfiguration>>()?.Value;

            var server = WireMockServer.Start(wireMockServerSettings!);

            Console.WriteLine($"WireMock server is running at {wireMockServerSettings?.Urls?[0]}");

            var stubFactory = new StubFactory(holdingsServiceConfiguration!, productKeyServiceConfiguration!);
            var stubManager = new StubManager(stubFactory, server);
            stubManager.RegisterStubs();

            // Keep the server running
            Console.ReadLine();
            server.Stop();
        }
    }
}