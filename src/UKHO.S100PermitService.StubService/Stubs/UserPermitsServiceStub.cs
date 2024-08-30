using System.Linq;
using System.Net;
using UKHO.S100PermitService.StubService.Configuration;
using WireMock;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace UKHO.S100PermitService.StubService.Stubs
{
    public class UserPermitsServiceStub : IStub
    {
        private readonly UserPermitsServiceConfiguration _userPermitsServiceConfiguration;
        private const string APPLICATIONTYPE = "application/json";
        private const string RESPONSEFILEDIRECTORY = "StubData\\UserPermits";
        private readonly string RESPONSEFILEDIRECTORYPATH = Path.Combine(Environment.CurrentDirectory, RESPONSEFILEDIRECTORY);
        public UserPermitsServiceStub(UserPermitsServiceConfiguration userPermitsServiceConfiguration)
        {
            _userPermitsServiceConfiguration = userPermitsServiceConfiguration;
        }

        public void ConfigureStub(WireMockServer server)
        {
            //401 - Unauthorized
            server
            .Given(Request.Create()
             .WithPath(new WildcardMatcher(_userPermitsServiceConfiguration.Url + "/*"))
             .UsingGet()
             .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch)
             .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString()))
            .RespondWith(Response.Create()
             .WithStatusCode(HttpStatusCode.Unauthorized)
             .WithBody(@"{ ""result"": ""token is missing""}"));

            server.Given
                (Request.Create()
                .WithPath(new WildcardMatcher(_userPermitsServiceConfiguration.Url + "/*")).UsingGet()
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
            .RespondWith(Response.Create().WithCallback(req =>
            {
                return SetResponse(req);
            }));

        }

        private ResponseMessage SetResponse(IRequestMessage request)
        {
            var licenceID = request.AbsolutePath.Split('/')[2];

            Int32.TryParse(licenceID, out var licenceId);

            var response = new ResponseMessage();
            var bodyData = new BodyData()
            {
                DetectedBodyType = BodyType.String
            };

            string filePath;
            switch(licenceId)
            {
                case 1://200 - OK
                case 2:
                case 3:
                case 4:
                case 5:

                    filePath = Path.Combine(RESPONSEFILEDIRECTORYPATH, $"response-200-licenceId-{licenceId}.json");
                    response.StatusCode = HttpStatusCode.OK;                    

                    break;

                case 0://400 - BadRequest

                    filePath = Path.Combine(RESPONSEFILEDIRECTORYPATH, $"response-400.json");                    
                    response.StatusCode = HttpStatusCode.BadRequest;

                    break;

                default: //404 - NotFound

                    filePath = Path.Combine(RESPONSEFILEDIRECTORYPATH, $"response-404.json");
                    response.StatusCode = HttpStatusCode.NotFound;
                    
                    break;
            }

            bodyData.BodyAsString = File.ReadAllText(filePath);
            response.BodyData = bodyData;
            response.AddHeader("Content-Type", APPLICATIONTYPE);
            response.AddHeader("X-Correlation-ID", Guid.NewGuid().ToString());
            return response;
        }
    }
}