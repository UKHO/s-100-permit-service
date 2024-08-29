using Microsoft.Extensions.Logging;
using System.Net;
using UKHO.S100PermitService.StubService.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace UKHO.S100PermitService.StubService.Stubs
{
    public class HoldingsServiceStub : IStub
    {
        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;
        private readonly ILogger<HoldingsServiceStub> _logger;

        public HoldingsServiceStub(HoldingsServiceConfiguration holdingsServiceConfiguration, ILogger<HoldingsServiceStub> logger)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration ?? throw new ArgumentNullException(nameof(holdingsServiceConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ConfigureStub(WireMockServer server)
        {
            _logger.LogInformation("Configuring HoldingsServiceStub...");

            server
                .Given(Request.Create().WithPath(_holdingsServiceConfiguration.Url).UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{ \"message\": \"PKS API Stub response\" }"));

            _logger.LogInformation("HoldingsServiceStub configured.");
        }
    }
}