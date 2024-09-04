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
        private const string ApplicationType = "application/json";
        private const string ResponseFileDirectory = @"StubData\Holdings";
        private readonly string _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);
        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;

        public HoldingsServiceStub(HoldingsServiceConfiguration holdingsServiceConfiguration)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration;
        }

        public void ConfigureStub(WireMockServer server)
        {
            server.Given
                (Request.Create()
                .WithPath(new WildcardMatcher(_holdingsServiceConfiguration.Url + "/*"))
                .UsingGet()
                .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString())
                .WithBodyFromFile(Path.Combine(_responseFileDirectoryPath, "response-401.json")));

            server
                .Given(Request.Create().WithPath(_holdingsServiceConfiguration.Url + "/*")
                .UsingGet()
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create().WithCallback(SetResponseFromLicenseId));
        }

        private ResponseMessage SetResponseFromLicenseId(IRequestMessage request)
        {
            var licenceId = ExtractLicenceId(request);

            var responseMessage = new ResponseMessage
            {
                BodyData = new BodyData
                {
                    DetectedBodyType = BodyType.String
                }
            };

            string filePath;
            switch(licenceId)
            {
                case int n when n >= 1 && n <= 5:
                    filePath = Path.Combine(_responseFileDirectoryPath, $"response-200-licenceid-{licenceId}.json");
                    responseMessage.StatusCode = HttpStatusCode.OK;
                    break;

                case 0:
                    filePath = Path.Combine(_responseFileDirectoryPath, "response-400.json");
                    responseMessage.StatusCode = HttpStatusCode.BadRequest;
                    break;

                default:
                    filePath = Path.Combine(_responseFileDirectoryPath, "response-404.json");
                    responseMessage.StatusCode = HttpStatusCode.NotFound;
                    break;
            }

            responseMessage.BodyData.BodyAsString = File.ReadAllText(filePath);
            responseMessage.AddHeader("Content-Type", ApplicationType);
            responseMessage.AddHeader("X-Correlation-ID", Guid.NewGuid().ToString());

            return responseMessage;
        }

        private static int ExtractLicenceId(IRequestMessage request)
        {
            var value = request.AbsolutePath.Split('/')[2];
            return int.TryParse(value, out var licenceId) ? licenceId : 0;
        }
    }
}