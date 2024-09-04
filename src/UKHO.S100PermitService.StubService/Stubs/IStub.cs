using WireMock.Server;

namespace UKHO.S100PermitService.StubService.Stubs
{
    public interface IStub
    {
        void ConfigureStub(WireMockServer server);
    }
}