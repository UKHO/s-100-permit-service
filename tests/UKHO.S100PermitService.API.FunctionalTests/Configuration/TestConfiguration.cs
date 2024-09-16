using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
            var keyVaultUri = configurationRoot["KeyVaultSettings:ServiceUri"]!;

            if(!string.IsNullOrWhiteSpace(keyVaultUri))
            {
                var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
                configBuilder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                configurationRoot = configBuilder.Build(); // Rebuild configuration to include KeyVault secrets
            }
            return configurationRoot;
        }

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