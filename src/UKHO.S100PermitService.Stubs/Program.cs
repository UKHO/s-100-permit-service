using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Stubs.Configuration;
using WireMock.Server;
using WireMock.Settings;

namespace UKHO.S100PermitService.Stubs
{
    internal class Program
    {
        private static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            services.Configure<ApiStubConfiguration>(configuration.GetSection("ApiStubConfiguration"));
            services.Configure<ShopFacadeApi>(configuration.GetSection("ShopFacadeApi"));
            services.Configure<ProductKeyServiceApi>(configuration.GetSection("ProductKeyServiceApi"));

            var serviceProvider = services.BuildServiceProvider();

            var stubConfig = serviceProvider.GetService<IOptions<ApiStubConfiguration>>()?.Value;
            var shopFacadeApi = serviceProvider.GetService<IOptions<ShopFacadeApi>>()?.Value;
            var productKeyServiceApi = serviceProvider.GetService<IOptions<ProductKeyServiceApi>>()?.Value;

            var server = WireMockServer.Start(new WireMockServerSettings
            {
                Port = stubConfig?.Port,
                UseSSL = true
            });

            Console.WriteLine($"WireMock server is running at https://localhost:{stubConfig?.Port}");

            var factory = new ApiStubFactory(shopFacadeApi!, productKeyServiceApi!);
            var stubConfigurationManager = new ApiStubConfigurationManager(factory, server);
            stubConfigurationManager.RegisterStubs();

            // Keep the server running
            Console.ReadLine();
            server.Stop();
        }
    }
}