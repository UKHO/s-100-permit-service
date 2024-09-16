using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;

namespace UKHO.S100PermitService.API.FunctionalTests.Helpers
{
    public class TestBase
    {
        private readonly ServiceProvider? _serviceProvider;

        public ServiceProvider? GetServiceProvider()
        {
            return _serviceProvider;
        }

        public TestBase()
        {
            _serviceProvider = TestConfiguration.ConfigureServices();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
        }
    }
}