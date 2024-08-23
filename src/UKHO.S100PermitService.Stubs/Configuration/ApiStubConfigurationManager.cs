using WireMock.Server;

namespace UKHO.S100PermitService.Stubs.Configuration
{
    public class ApiStubConfigurationManager
    {
        private readonly ApiStubFactory _apiStubFactory;
        private readonly WireMockServer _server;

        public ApiStubConfigurationManager(ApiStubFactory apiStubFactory, WireMockServer server)
        {
            _apiStubFactory = apiStubFactory;
            _server = server;
        }

        public void RegisterStubs()
        {
            var shopFacadeApiStub = _apiStubFactory.CreateShopFacadeApiStub();
            var productKeyServiceApiStub = _apiStubFactory.CreateProductKeyServiceApiStub();

            shopFacadeApiStub.ConfigureStub(_server);
            productKeyServiceApiStub.ConfigureStub(_server);
        }
    }
}
