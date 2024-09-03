using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.StubService.Configuration;
using UKHO.S100PermitService.StubService.StubSetup;
using WireMock.Server;
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

        //private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        //{
        //    services.AddLogging(logging => logging.AddConsole().AddDebug());

        //    services.AddTransient<IWireMockService, WireMockService>();
        //    services.Configure<WireMockServerSettings>(configuration.GetSection("WireMockServerSettings"));

        //    services.AddHostedService<App>();
        //}

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //configuration
            //    .AddJsonFile("appsettings.json")
            //    .Build();

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