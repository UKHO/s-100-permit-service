using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UKHO.S100PermitService.API.FunctionalTests.Configuration
{
    public static class TestConfiguration
    {
        /// <summary>
        /// This method is used to load the app settings configuration 
        /// </summary>
        /// <returns></returns>
        public static IConfigurationRoot LoadConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false);

            var configurationRoot = configBuilder.Build();
            return configurationRoot;
        }

        /// <summary>
        /// This method is used to load and set the values of app settings configs. 
        /// </summary>
        /// <returns></returns>
        public static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddOptions();

            var configurationRoot = LoadConfiguration();

            services.Configure<PermitServiceApiConfiguration>(configurationRoot.GetSection("PermitServiceApiConfiguration"));
            services.Configure<TokenConfiguration>(configurationRoot.GetSection("TokenConfiguration"));

            return services.BuildServiceProvider();
        }
    }
}