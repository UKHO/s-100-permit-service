using UKHO.S100PermitService.StubService.Stubs;
using WireMock.Server;

namespace UKHO.S100PermitService.StubService.StubSetup
{
    public class StubManager
    {
        private readonly StubFactory _stubFactory;
        private readonly WireMockServer _wireMockServer;

        public StubManager(StubFactory stubFactory, WireMockServer wireMockServer)
        {
            _stubFactory = stubFactory;
            _wireMockServer = wireMockServer;
        }

        public void RegisterStubs()
        {
            RegisterStub(_stubFactory.CreateHoldingsServiceStub());
            RegisterStub(_stubFactory.CreateProductKeyServiceStub());
            RegisterStub(_stubFactory.CreateUserPermitsServiceStub());
        }

        private void RegisterStub<T>(T stub) where T : IStub
        {
            stub.ConfigureStub(_wireMockServer);
        }
    }
}