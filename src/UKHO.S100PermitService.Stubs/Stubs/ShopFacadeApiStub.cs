using UKHO.S100PermitService.Stubs.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace UKHO.S100PermitService.Stubs.Stubs
{
    public class ShopFacadeApiStub : IApiStub
    {
        private readonly ShopFacadeApi _shopFacadeApi;

        public ShopFacadeApiStub(ShopFacadeApi shopFacadeApi)
        {
            _shopFacadeApi = shopFacadeApi;
        }

        public void ConfigureStub(WireMockServer server)
        {
            server
                .Given(Request.Create().WithPath(_shopFacadeApi.Url).UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type" , "application/json")
                    .WithBody("{ \"message\": \"ShopFacade API Stub response\" }"));
        }
    }
}
