using WireMock.Server;

namespace UKHO.S100PermitService.StubService.Configuration
{
    public class StubConfigurationManager
    {
        private readonly StubFactory _stubFactory;
        private readonly WireMockServer _server;

        public StubConfigurationManager(StubFactory stubFactory, WireMockServer server)
        {
            _stubFactory = stubFactory;
            _server = server;
        }

        public void RegisterStubs()
        {
            var productKeyServiceStub = _stubFactory.CreateProductKeyServiceStub();

            productKeyServiceStub.ConfigureStub(_server);
        }
    }
}
