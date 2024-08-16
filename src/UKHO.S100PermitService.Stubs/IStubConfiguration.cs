using WireMock.Server;

namespace UKHO.S100PermitService.Stubs
{
    interface IStubConfiguration
    {
        void ConfigureStub(WireMockServer server);
    }
}
