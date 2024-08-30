using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.StubSetup;
using WireMock.Server;
using WireMock.Settings;

namespace UKHO.S100PermitService.StubService
{
    public class Program
    {
        public static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            services.AddLogging(configure => configure.AddConsole());

            services.Configure<StubConfiguration>(configuration.GetSection("StubConfiguration"));
            services.Configure<HoldingsServiceConfiguration>(configuration.GetSection("HoldingsServiceConfiguration"));
            services.Configure<ProductKeyServiceConfiguration>(configuration.GetSection("ProductKeyServiceConfiguration"));
            services.Configure<UserPermitsServiceConfiguration>(configuration.GetSection("UserPermitsServiceConfiguration"));

            var serviceProvider = services.BuildServiceProvider();

            var _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var stubConfiguration = serviceProvider.GetService<IOptions<StubConfiguration>>()?.Value;
            var holdingsServiceConfiguration = serviceProvider.GetService<IOptions<HoldingsServiceConfiguration>>()?.Value;
            var productKeyServiceConfiguration = serviceProvider.GetService<IOptions<ProductKeyServiceConfiguration>>()?.Value;
            var userPermitsServiceConfiguration = serviceProvider.GetService<IOptions<UserPermitsServiceConfiguration>>()?.Value;

            var server = WireMockServer.Start(new WireMockServerSettings
            {
                Port = stubConfiguration?.Port,
                ReadStaticMappings = true,
                WatchStaticMappings = true,
                WatchStaticMappingsInSubdirectories = true,
                UseSSL = true
            });

            Console.WriteLine($"WireMock server is running at https://localhost:{stubConfiguration?.Port}");

            var stubFactory = new StubFactory(holdingsServiceConfiguration!, productKeyServiceConfiguration!, userPermitsServiceConfiguration!);
            var stubManager = new StubManager(stubFactory, server);
            stubManager.RegisterStubs();

            // Keep the server running
            Console.ReadLine();
            server.Stop();
        }
    }
}