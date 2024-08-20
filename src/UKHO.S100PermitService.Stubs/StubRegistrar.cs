using WireMock.Server;

namespace UKHO.S100PermitService.Stubs
{
    public class StubRegistrar
    {
        private readonly ApiStubFactory _apiStubFactory
            ;
        private readonly WireMockServer _server;

        public StubRegistrar(ApiStubFactory apiStubFactory , WireMockServer server)
        {
            _apiStubFactory = apiStubFactory;
            _server = server;
        }

        public void RegisterStubs()
        {
            var shopFacadeApiStub = _apiStubFactory.CreateShopFacadeApiStub();
            var productKeyServiceApiStub = _apiStubFactory.CreateProductKeyServiceApiStub();

            shopFacadeApiStub.Register(_server);
            productKeyServiceApiStub.Register(_server);
        }
    }

}
