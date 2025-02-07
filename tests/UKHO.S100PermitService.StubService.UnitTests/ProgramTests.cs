using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.S100PermitService.StubService.Configuration;
using WireMock.Server;
using WireMock.Settings;

namespace UKHO.S100PermitService.StubService.UnitTests
{
    [TestFixture]
    public class ProgramTests
    {
        private IConfiguration _configuration;
        private ServiceCollection _services;
        private ServiceProvider _serviceProvider;

        [SetUp]
        public void SetUp()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _services = new ServiceCollection();
            _services.AddLogging(configure => configure.AddConsole());
            _services.Configure<WireMockServerSettings>(_configuration.GetSection("WireMockServerSettings"));
            _services.Configure<ProductKeyServiceConfiguration>(_configuration.GetSection("ProductKeyServiceConfiguration"));

            _serviceProvider = _services.BuildServiceProvider();
        }

        [TearDown]
        public void CleanUp()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public void WhenWireMockServerIsStarted_ThenServerIsRunning()
        {
            var server = WireMockServer.Start(new WireMockServerSettings
            {
                ReadStaticMappings = true,
                WatchStaticMappings = true,
                WatchStaticMappingsInSubdirectories = true,
                UseSSL = true
            });

            var isRunning = server.IsStarted;

            isRunning.Should().BeTrue();

            server.Should().NotBeNull();
            server.Stop();
        }

        [Test]
        public void WhenConfigurationsAreLoaded_ThenConfigurationsAreNotMissing()
        {
            var stubConfiguration = _serviceProvider.GetService<IOptions<WireMockServerSettings>>()?.Value;
            var productKeyServiceConfiguration = _serviceProvider.GetService<IOptions<ProductKeyServiceConfiguration>>()?.Value;

            stubConfiguration.Should().NotBeNull();
            productKeyServiceConfiguration.Should().NotBeNull();
        }
    }
}