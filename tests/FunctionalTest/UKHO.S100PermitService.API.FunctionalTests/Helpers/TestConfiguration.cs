using Microsoft.Extensions.Configuration;

namespace UKHO.S100PermitService.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot configurationRoot;
        public PermitServiceApiConfiguration permitServiceConfig = new();

        public class PermitServiceApiConfiguration
        {
            public string? BaseUrl { get; set; }
        }

        public TestConfiguration()
        {
            configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();
            configurationRoot.Bind("PermitServiceApiConfiguration", permitServiceConfig);
        }
    }
}