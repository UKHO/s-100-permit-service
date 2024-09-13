using System.Net;
using UKHO.S100PermitService.StubService.Configuration;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

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
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader(HttpHeaderConstants.ContentType, HttpHeaderConstants.ApplicationType)
                .WithHeader(HttpHeaderConstants.CorrelationId, Guid.NewGuid().ToString())
                .WithBodyFromFile(Path.Combine(ResponseFileDirectory, "response-200.json")));

            server //400 when incorrect request passed
                .Given(Request.Create()
                .WithPath(new WildcardMatcher(_productKeyServiceConfiguration.Url, true))
                .UsingPost()
                .WithBody(new JsonMatcher(GetJsonData(Path.Combine(ResponseFileDirectory, "request-400.json"))))
                .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithHeader(HttpHeaderConstants.ContentType, HttpHeaderConstants.ApplicationType)
                .WithHeader(HttpHeaderConstants.CorrelationId, Guid.NewGuid().ToString()));
        }

        private static string GetJsonData(string filePath)
        {
            using var fileStream = new StreamReader(filePath);
            return fileStream.ReadToEnd();
        }
    }
}