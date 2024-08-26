using WireMock.Server;

namespace UKHO.S100PermitService.StubService
{
    public interface IStub
    {
        void ConfigureStub(WireMockServer server);
    }
}
