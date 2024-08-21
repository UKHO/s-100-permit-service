using UKHO.S100PermitService.Stubs.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace UKHO.S100PermitService.Stubs.Stubs
{
    public class ProductKeyServiceApiStub : IApiStub
    {
        private readonly ProductKeyServiceApi _productKeyServiceApi;

        public ProductKeyServiceApiStub(ProductKeyServiceApi productKeyServiceApi)
        {
            _productKeyServiceApi = productKeyServiceApi;
        }

        public void ConfigureStub(WireMockServer server)
        {
            server
                .Given(Request.Create().WithPath(_productKeyServiceApi.Url).UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type" , "application/json")
                    .WithBody("{ \"message\": \"PKS API Stub response\" }"));
        }

    }
}
