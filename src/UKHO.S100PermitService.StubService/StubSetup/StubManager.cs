using UKHO.S100PermitService.StubService.Stubs;
using WireMock.Server;

namespace UKHO.S100PermitService.StubService.StubSetup
{
    public class StubManager
    {
        private readonly StubCreator _stubCreator;
        private readonly WireMockServer _wireMockServer;

        public StubManager(StubCreator stubCreator, WireMockServer wireMockServer)
        {
            _stubCreator = stubCreator;
            _wireMockServer = wireMockServer;
        }

        public void RegisterStubs()
        {
            RegisterStub(_stubCreator.CreateHoldingsServiceStub());
            RegisterStub(_stubCreator.CreateProductKeyServiceStub());
        }

        private void RegisterStub<T>(T stub) where T : IStub
        {
            stub.ConfigureStub(_wireMockServer);
        }
    }
}