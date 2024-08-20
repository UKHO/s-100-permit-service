using WireMock.Server;

namespace UKHO.S100PermitService.Stubs
{
    public interface IApiStub
    {
        void Register(WireMockServer server);
    }
}
