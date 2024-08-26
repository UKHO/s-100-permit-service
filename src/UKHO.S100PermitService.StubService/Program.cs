using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.StubService.Configuration;
using WireMock.Server;
using WireMock.Settings;

namespace UKHO.S100PermitService.StubService
{
    internal class Program
    {
        private static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            services.Configure<StubConfiguration>(configuration.GetSection("ApiStubConfiguration"));
            services.Configure<ProductKeyServiceConfiguration>(configuration.GetSection("ProductKeyServiceApi"));

            var serviceProvider = services.BuildServiceProvider();

            var stubConfig = serviceProvider.GetService<IOptions<StubConfiguration>>()?.Value;
            var productKeyServiceApi = serviceProvider.GetService<IOptions<ProductKeyServiceConfiguration>>()?.Value;

            var server = WireMockServer.Start(new WireMockServerSettings
            {
                Port = stubConfig?.Port,
                UseSSL = true
            });

            Console.WriteLine($"WireMock server is running at https://localhost:{stubConfig?.Port}");

            var stubFactory = new StubFactory(productKeyServiceApi!);
            var stubConfigurationManager = new StubConfigurationManager(stubFactory, server);
            stubConfigurationManager.RegisterStubs();

            // Keep the server running
            Console.ReadLine();
            server.Stop();
        }
    }
}