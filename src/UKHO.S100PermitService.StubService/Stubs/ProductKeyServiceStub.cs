using System.Net;
using UKHO.S100PermitService.StubService.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace UKHO.S100PermitService.StubService.Stubs
{
    public class ProductKeyServiceStub : IStub
    {
        private readonly ProductKeyServiceConfiguration _productKeyServiceApi;

        public ProductKeyServiceStub(ProductKeyServiceConfiguration productKeyServiceApi)
        {
            _productKeyServiceApi = productKeyServiceApi;
        }

        public void ConfigureStub(WireMockServer server)
        {
            server
                .Given(Request.Create().WithPath(_productKeyServiceApi.Url).UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{ \"message\": \"PKS API Stub response\" }"));
        }
    }
}
