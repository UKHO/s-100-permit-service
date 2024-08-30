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

        public HoldingsServiceStub(HoldingsServiceConfiguration holdingsServiceConfiguration)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration ?? throw new ArgumentNullException(nameof(holdingsServiceConfiguration));
        }

        public void ConfigureStub(WireMockServer server)
        {
            server
                .Given(Request.Create().WithPath(_holdingsServiceConfiguration.Url).UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{ \"message\": \"PKS API Stub response\" }"));
        }
    }
}