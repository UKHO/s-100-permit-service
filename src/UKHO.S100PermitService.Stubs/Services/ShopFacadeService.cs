using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace UKHO.S100PermitService.Stubs.Service
{
    public class ShopFacadeService : IStubConfiguration
    {
        public const string ShopFacadeUrl = "/shop-facade/upns";
        public const string ContentType = "Content-Type";
        public const string ApplicationType = "application/json";
        public void ConfigureStub(WireMockServer server)
        {
            server.Given(Request.Create().WithPath(ShopFacadeUrl).UsingGet())
                  .RespondWith(Response.Create().WithStatusCode(200).WithBody("Response for Shop Facade api"));

        }
    }
}
