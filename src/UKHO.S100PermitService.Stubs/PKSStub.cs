using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace UKHO.S100PermitService.Stubs
{
    public class PKSStub : IStubService
    {
        public const string PksUrl = "/keys/ENC-S63";
        public const string ContentType = "Content-Type";
        public const string ApplicationType = "application/json";
        public void ConfigureStub(WireMockServer server)
        {
            //200 - OK 
            server.Given(Request.Create().WithPath(PksUrl).UsingGet())
                  .RespondWith(Response.Create().WithStatusCode(200).WithBody("Response for PKS"));
        }

            
    }
}
