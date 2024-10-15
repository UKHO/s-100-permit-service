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
    public class ProductKeyServiceStub : IStub
    {
        private const string ResponseFileDirectory = @"StubData\ProductKeyService";

        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;

        public ProductKeyServiceStub(ProductKeyServiceConfiguration productKeyServiceConfiguration)
        {
            _productKeyServiceConfiguration = productKeyServiceConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceConfiguration));
        }

        public void ConfigureStub(WireMockServer server)
        {
            server //401
                 .Given(Request.Create()
                 .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                 .UsingPost()
                 .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch))
                 .RespondWith(Response.Create()
                 .WithStatusCode(HttpStatusCode.Unauthorized)
                 .WithHeader(HttpHeaderConstants.ContentType, HttpHeaderConstants.ApplicationType)
                 .WithHeader(HttpHeaderConstants.CorrelationId, Guid.NewGuid().ToString())
                 .WithBodyFromFile(Path.Combine(ResponseFileDirectory, "response-401.json")));

            server //404 when invalid or non-existent cell passed
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NotFound)
                .WithHeader(HttpHeaderConstants.ContentType, HttpHeaderConstants.ApplicationType)
                .WithHeader(HttpHeaderConstants.CorrelationId, Guid.NewGuid().ToString())
                .WithBodyFromFile(Path.Combine(ResponseFileDirectory, "response-datanotfound-404.json")));

            server //404 when cell is correct but data is not available on pks service
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-404.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NotFound)
                .WithHeader(HttpHeaderConstants.ContentType, HttpHeaderConstants.ApplicationType)
                .WithHeader(HttpHeaderConstants.CorrelationId, Guid.NewGuid().ToString())
                .WithBodyFromFile(Path.Combine(ResponseFileDirectory, "response-404.json")));

            server //200
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-200.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-200.json", HttpStatusCode.OK)));

            server //400 when incorrect request passed
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-400.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-400.json", HttpStatusCode.BadRequest)));
        }

        private static string GetJsonData(string filePath)
        {
            using var fileStream = new StreamReader(filePath);
            return fileStream.ReadToEnd();
        }

        private static ResponseMessage CreateResponse(IRequestMessage request, string fileName, HttpStatusCode statusCode)
        {
            var correlationId = GetCorrelationId(request);
            var responseBody = GetUpdatedResponse(fileName, statusCode, correlationId);

            var responseMessage = new ResponseMessage
            {
                BodyData = new BodyData
                {
                    DetectedBodyType = BodyType.String
                }
            };

            responseMessage.StatusCode = statusCode;
            responseMessage.BodyData.BodyAsString = responseBody;
            responseMessage.AddHeader(HttpHeaderConstants.ContentType, HttpHeaderConstants.ApplicationType);
            responseMessage.AddHeader(HttpHeaderConstants.CorrelationId, correlationId);

            return responseMessage;
        }

        private static string GetUpdatedResponse(string fileName, HttpStatusCode statusCode, string correlationId)
        {
            var filePath = Path.Combine(ResponseFileDirectory, fileName);
            return ResponseHelper.UpdateCorrelationIdInResponse(filePath, correlationId, statusCode);
        }

        private static string GetCorrelationId(IRequestMessage request)
        {
            if(request.Headers!.TryGetValue(HttpHeaderConstants.CorrelationId, out var correlationId) && correlationId?.FirstOrDefault() != null)
            {
                return correlationId.First();
            }
            return Guid.NewGuid().ToString();
        }
    }
}