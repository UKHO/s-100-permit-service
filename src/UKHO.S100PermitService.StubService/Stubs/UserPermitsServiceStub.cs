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
        int[] VALIDLICENCEIDs = { 1, 2, 3, 4 };

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
            //server.Given(Request.Create().WithPath(_userPermitsServiceConfiguration.Url).WithParam("licenceid","2").UsingGet())
            //.RespondWith(Response.Create().WithCallback(req =>
            //{
            //    return SetResponse(req);
            //}));

            //server.Given(Request.Create().WithPath(_userPermitsServiceConfiguration.Url + "/2").UsingGet()
            //    .WithHeader("correlationId","*"))
            //.RespondWith(Response.Create().WithCallback(req =>
            //{
            //    return SetResponse(req);
            //}));

            server.Given(Request.Create().WithPath(new WildcardMatcher(_userPermitsServiceConfiguration.Url + "/*")).UsingGet()
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch)
                .WithHeader("correlationId", "*"))
            .RespondWith(Response.Create().WithCallback(req =>
            {
                return SetResponse(req);
            }));

            ////400 - BadRequest
            //server.Given(Request.Create().WithPath(new WildcardMatcher(_userPermitsServiceConfiguration.Url + "/*")).UsingGet())
            //.RespondWith(Response.Create().WithCallback(req =>
            //{
            //    return SetResponse(req);
            //}));

            //401 - Unauthorized
            server
            .Given(Request.Create()
             .WithPath(new WildcardMatcher(_userPermitsServiceConfiguration.Url + "/*"))
             .UsingGet()
             .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch))
            .RespondWith(Response.Create()
             .WithStatusCode(HttpStatusCode.Unauthorized)
             .WithBody(@"{ ""result"": ""token is missing""}"));

            //server.Given(Request.Create().WithPath(new WildcardMatcher(_userPermitsServiceConfiguration.Url + "/*")).UsingGet()
            //    .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
            //.RespondWith(Response.Create().WithCallback(req =>
            //{
            //    return SetResponse(req);
            //}));

        }

        private ResponseMessage SetResponse(IRequestMessage request)
        {
            var licenceID = request.AbsolutePath.Split('/')[2];

            var responseCode = Int32.TryParse(licenceID, out int result) ? VALIDLICENCEIDs.Contains(result) ? 200 : 404 : 400;
            var response = new ResponseMessage();
            var bodyData = new BodyData()
            {
                DetectedBodyType = BodyType.String
            };

            //var scenario = Convert.ToString(request.Query["licenceid"]);
            string fileContent;
            switch(responseCode)
            {
                case 200://200 - OK

                    fileContent = Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "StubData//UserPermits", $"response-200-licenceid-{result}.json");

                    response.StatusCode = HttpStatusCode.OK;
                    bodyData.BodyAsString = File.ReadAllText(fileContent);

                    break;

                case 404://404 - NotFound
                    fileContent = Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "StubData//UserPermits", "response-404.json");

                    response.StatusCode = HttpStatusCode.NotFound;
                    bodyData.BodyAsString = File.ReadAllText(fileContent);
                    break;

                case 400://400 - BadRequest

                    fileContent = Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "StubData//UserPermits", "response-400.json");

                    bodyData.BodyAsString = File.ReadAllText(fileContent);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    break;

                default:
                    break;
            }

            var correlationId = !string.IsNullOrEmpty(request?.Headers?["correlationId"]?[0]) ? request.Headers["correlationId"][0] : Guid.NewGuid().ToString();

            response.BodyData = bodyData;
            response.AddHeader("Content-Type", APPLICATIONTYPE);
            response.AddHeader("correlationId", correlationId);
            return response;
        }
    }
}