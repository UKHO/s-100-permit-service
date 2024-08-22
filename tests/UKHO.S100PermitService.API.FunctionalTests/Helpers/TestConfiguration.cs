using Microsoft.Extensions.Configuration;

namespace UKHO.S100PermitService.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot _configurationRoot;
        public PermitServiceApiConfiguration PermitServiceConfig { get; private set; }

        public class PermitServiceApiConfiguration
        {
            public string? BaseUrl { get; set; }
        }

        public TestConfiguration()
        {
            _configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();
            _configurationRoot.Bind("PermitServiceApiConfiguration", PermitServiceConfig);
        }
    }
}
