using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UKHO.S100PermitService.API.FunctionalTests.Configuration
{
    public static class TestConfiguration
    {
        public static IConfigurationRoot LoadConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false);

            var configurationRoot = configBuilder.Build();
            return configurationRoot;
        }

        public static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddOptions();

            var configurationRoot = LoadConfiguration();

            services.Configure<PermitServiceApiConfiguration>(configurationRoot.GetSection("PermitServiceApiConfiguration"));

            return services.BuildServiceProvider();
        }
    }
}