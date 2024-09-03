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
    public class HoldingsServiceStub : IStub
    {
        private const string APPLICATIONTYPE = "application/json";
        private const string RESPONSEFILEDIRECTORY = "StubData\\Holdings";
        private readonly string responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, RESPONSEFILEDIRECTORY);

        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;

        public HoldingsServiceStub(HoldingsServiceConfiguration holdingsServiceConfiguration)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration;
        }

        public void ConfigureStub(WireMockServer server)
        {
            //401 - Unauthorized
            server.Given
                (Request.Create()
                .WithPath(new WildcardMatcher(_holdingsServiceConfiguration.Url + "/*"))
                .UsingGet()
                .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch)
                .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString()))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithBody(@"{ ""result"": ""token is missing""}"));

            server
                .Given(Request.Create().WithPath(_holdingsServiceConfiguration.Url + "/*")
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch)
                .WithHeader("X-Correlation-ID", "*"))
                .RespondWith(Response.Create().WithCallback(SetResponse));
        }

        private ResponseMessage SetResponse(IRequestMessage request)
        {
            var value = request.AbsolutePath.Split('/')[2];
            int.TryParse(value, out var licenceId);

            var responseMessage = new ResponseMessage();
            var bodyData = new BodyData()
            {
                DetectedBodyType = BodyType.String
            };

            string filePath;
            switch(licenceId)
            {
                //200 - OK
                case int n when n >= 1 && n <= 5:
                    filePath = Path.Combine(responseFileDirectoryPath, $"response-200-licenceid-{licenceId}.json");
                    responseMessage.StatusCode = HttpStatusCode.OK;
                    break;

                //400 - BadRequest
                case 0:
                    filePath = Path.Combine(responseFileDirectoryPath, "response-400.json");
                    responseMessage.StatusCode = HttpStatusCode.BadRequest;
                    break;

                //404 - NotFound
                default:
                    filePath = Path.Combine(responseFileDirectoryPath, "response-404.json");
                    responseMessage.StatusCode = HttpStatusCode.NotFound;
                    break;
            }

            bodyData.BodyAsString = File.ReadAllText(filePath);
            responseMessage.BodyData = bodyData;
            responseMessage.AddHeader("Content-Type", APPLICATIONTYPE);
            responseMessage.AddHeader("X-Correlation-ID", Guid.NewGuid().ToString());
            return responseMessage;
        }
    }
}