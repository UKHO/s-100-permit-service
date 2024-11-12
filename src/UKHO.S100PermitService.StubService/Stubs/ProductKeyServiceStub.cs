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
                 .WithCallback(request => CreateResponse(request, "response-401.json", HttpStatusCode.Unauthorized)));

            server //404 when invalid or non-existent cell passed
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-datanotfound-404.json", HttpStatusCode.NotFound)));

            server //404 when cell is correct but data is not available on pks service
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-404.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-404.json", HttpStatusCode.NotFound)));

            server //200
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-200.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-200.json", HttpStatusCode.OK)));

            server //200
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-200-8-DuplicateCell.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-200-8-DuplicateCell.json", HttpStatusCode.OK)));

            server //200
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-200-9-DuplicateCell.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-200-9-DuplicateCell.json", HttpStatusCode.OK)));

            server //200
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-200-11-DuplicateCell.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-200-11-DuplicateCell.json", HttpStatusCode.OK)));

            server //200 for 50 Holdings scenario
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-200-50.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-200-50.json", HttpStatusCode.OK)));

            server //200
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-200-12-DuplicateCell.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithCallback(request => CreateResponse(request, "response-200-12-DuplicateCell.json", HttpStatusCode.OK)));

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
            var correlationId = ResponseHelper.ExtractCorrelationId(request);

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
    }
}