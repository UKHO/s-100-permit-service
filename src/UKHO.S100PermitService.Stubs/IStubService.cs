using WireMock.Server;

namespace UKHO.S100PermitService.Stubs
{
    interface IStubService
    {
        void ConfigureStub(WireMockServer server);
    }
}
