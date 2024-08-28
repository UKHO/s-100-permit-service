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

        public UserPermitsServiceStub(UserPermitsServiceConfiguration userPermitsServiceConfiguration)
        {
            _userPermitsServiceConfiguration = userPermitsServiceConfiguration;
        }

        public void ConfigureStub(WireMockServer server)
        {
            //server
            //    .Given(Request.Create().WithPath(_userPermitsServiceConfiguration.Url).UsingGet())
            //    .RespondWith(Response.Create()
            //        .WithStatusCode(HttpStatusCode.OK)
            //        .WithHeader("Content-Type", "application/json")
            //        .WithBody("{ \"message\": \"User Permits API Stub response\" }"));

            //200 - OK
            //server.Given(Request.Create().WithPath(_userPermitsServiceConfiguration.Url + "/1").UsingGet())
            //.RespondWith(Response.Create().WithCallback(req =>
            //{
            //    return SetResponse(req);
            //}));

            //404 - NotFound
            server.Given(Request.Create().WithPath(_userPermitsServiceConfiguration.Url + "/2").UsingGet())
            .RespondWith(Response.Create().WithCallback(req =>
            {
                return SetResponse(req);
            }));

            //400 - BadRequest
            server.Given(Request.Create().WithPath(_userPermitsServiceConfiguration.Url + "/error").UsingGet())
            .RespondWith(Response.Create().WithCallback(req =>
            {
                return SetResponse(req);
            }));

            //401 - Unauthorized
            server
            .Given(Request.Create()
             .WithPath(_userPermitsServiceConfiguration.Url + "/1")
             .UsingGet()
             .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch))
            .RespondWith(Response.Create()
             .WithStatusCode(HttpStatusCode.Unauthorized)
             .WithBody(@"{ ""result"": ""token missing""}"));

            server.Given(Request.Create().WithPath(_userPermitsServiceConfiguration.Url + "/1").UsingGet()
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
            .RespondWith(Response.Create().WithCallback(req =>
            {
                return SetResponse(req);
            }));

        }

        private static ResponseMessage SetResponse(IRequestMessage request)
        {
            var scenario = request.AbsolutePath.Split('/')[2];
            var response = new ResponseMessage();
            var bodyData = new BodyData()
            {
                DetectedBodyType = BodyType.String
            };

            switch(scenario)
            {
                case "1"://200 - OK

                    var fileContent = Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "StubData//UserPermits", "response-200-licenceid-1.json");

                    response.StatusCode = HttpStatusCode.OK;
                    bodyData.BodyAsString = File.ReadAllText(fileContent);

                    break;

                case "2"://404 - NotFound

                    bodyData.BodyAsString = "LicenseID not found or invalid";
                    response.StatusCode = HttpStatusCode.NotFound;
                    break;

                case "error"://400 - BadRequest

                    bodyData.BodyAsString = "Bad Request";
                    response.StatusCode = HttpStatusCode.BadRequest;
                    break;

                default:
                    break;
            }

            response.BodyData = bodyData;
            response.AddHeader("Content-Type", APPLICATIONTYPE);
            return response;
        }
    }
}