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
        private const string ResponseFileDirectory = @"StubData\Holdings";
        private const string ProductStandard = "/s100";

        private readonly string _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);
        private readonly HoldingsServiceConfiguration _holdingsServiceConfiguration;

        public HoldingsServiceStub(HoldingsServiceConfiguration holdingsServiceConfiguration)
        {
            _holdingsServiceConfiguration = holdingsServiceConfiguration ?? throw new ArgumentNullException(nameof(holdingsServiceConfiguration));
        }

        public void ConfigureStub(WireMockServer server)
        {
            server
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_holdingsServiceConfiguration.Url + "/*" + ProductStandard, true))
                .UsingGet()
                .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch))
                .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithHeader(HttpHeaderConstants.CorrelationId, Guid.NewGuid().ToString())
                .WithHeader(HttpHeaderConstants.ContentType, HttpHeaderConstants.ApplicationType)
                .WithBodyFromFile(Path.Combine(_responseFileDirectoryPath, "response-401.json")));

            server
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_holdingsServiceConfiguration.Url + "/*" + ProductStandard, true))
                .UsingGet()
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create().WithCallback(SetResponseFromLicenseId));
        }

        private ResponseMessage SetResponseFromLicenseId(IRequestMessage request)
        {
            var licenceId = ResponseHelper.ExtractLicenceId(request);

            var validLicenceIds = _holdingsServiceConfiguration.ValidLicenceIds;

            var correlationId = ResponseHelper.ExtractCorrelationId(request);

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
                case var n when validLicenceIds.Contains(n):
                    filePath = Path.Combine(_responseFileDirectoryPath, $"response-200-licenceid-{licenceId}.json");
                    responseMessage.StatusCode = HttpStatusCode.OK;
                    break;

                case 5:
                case 6:
                    filePath = Path.Combine(_responseFileDirectoryPath, $"response-204-licenceid-{licenceId}.json");
                    responseMessage.StatusCode = HttpStatusCode.NoContent;
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

            responseMessage.BodyData.BodyAsString = ResponseHelper.UpdateCorrelationIdInResponse(filePath, correlationId, (HttpStatusCode)responseMessage.StatusCode);
            responseMessage.AddHeader(HttpHeaderConstants.ContentType, HttpHeaderConstants.ApplicationType);
            responseMessage.AddHeader(HttpHeaderConstants.CorrelationId, correlationId);

            return responseMessage;
        }
    }
}