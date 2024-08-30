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
            _services.Configure<StubConfiguration>(_configuration.GetSection("StubConfiguration"));
            _services.Configure<HoldingsServiceConfiguration>(_configuration.GetSection("HoldingsServiceConfiguration"));
            _services.Configure<ProductKeyServiceConfiguration>(_configuration.GetSection("ProductKeyServiceConfiguration"));
            _services.Configure<UserPermitsServiceConfiguration>(_configuration.GetSection("UserPermitsServiceConfiguration"));

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
            var stubConfiguration = _serviceProvider.GetService<IOptions<StubConfiguration>>()?.Value;

            var server = WireMockServer.Start(new WireMockServerSettings
            {
                Port = stubConfiguration?.Port,
                ReadStaticMappings = true,
                WatchStaticMappings = true,
                WatchStaticMappingsInSubdirectories = true,
                UseSSL = true
            });

            var isRunning = server.IsStarted;

            isRunning.Should().BeTrue();
            stubConfiguration?.Port.Should().Be(server.Ports[0]);

            server.Should().NotBeNull();
            server.Stop();
        }

        [Test]
        public void WhenConfigurationsAreLoadedThenTheyAreNotMissing()
        {
            var stubConfiguration = _serviceProvider.GetService<IOptions<StubConfiguration>>()?.Value;
            var holdingsServiceConfiguration = _serviceProvider.GetService<IOptions<HoldingsServiceConfiguration>>()?.Value;
            var productKeyServiceConfiguration = _serviceProvider.GetService<IOptions<ProductKeyServiceConfiguration>>()?.Value;
            var userPermitsServiceConfiguration = _serviceProvider.GetService<IOptions<UserPermitsServiceConfiguration>>()?.Value;

            stubConfiguration.Should().NotBeNull();
            holdingsServiceConfiguration.Should().NotBeNull();
            productKeyServiceConfiguration.Should().NotBeNull();
            userPermitsServiceConfiguration.Should().NotBeNull();
        }
    }
}