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
        public const string CONTENTTYPE = "Content-Type";
        public const string APPLICATIONTYPE = "application/json";
        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;

        public ProductKeyServiceStub(ProductKeyServiceConfiguration productKeyServiceConfiguration)
        {
            _productKeyServiceConfiguration = productKeyServiceConfiguration;
        }

        public void ConfigureStub(WireMockServer server)
        {
            server //401
             .Given(Request.Create()
             .WithPath(_productKeyServiceConfiguration.Url)
             .UsingPost()
             .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch))
             .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                .WithBody(@"{ ""result"": ""token missing""}"));

            server //404
                  .Given(Request.Create()
                  .WithPath(_productKeyServiceConfiguration.Url)
                  .UsingPost()
                  .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                  .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.NotFound)
                        .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                        .WithBodyFromFile(Path.Combine("StubData\\PKS", "response-Allinvalid-404.json")));

            server //404
                  .Given(Request.Create()
                  .WithPath(_productKeyServiceConfiguration.Url)
                  .UsingPost()
                  .WithBody(new JsonMatcher(GetJsonData(Path.Combine("StubData\\PKS", "request-404.json"))))
                  .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                  .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.NotFound)
                        .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                        .WithBodyFromFile(Path.Combine("StubData\\PKS", "response-404.json")));

            server //200
                  .Given(Request.Create()
                  .WithPath(_productKeyServiceConfiguration.Url)
                  .UsingPost()
                  .WithBody(new JsonMatcher(GetJsonData(Path.Combine("StubData\\PKS", "request-200.json"))))
                  .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                  .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                        .WithBodyFromFile(Path.Combine("StubData\\PKS", "response-200.json")));

            server //400
                  .Given(Request.Create()
                  .WithPath(_productKeyServiceConfiguration.Url)
                  .UsingPost()
                  .WithBody(new JsonMatcher(GetJsonData(Path.Combine("StubData\\PKS", "request-400.json"))))
                  .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                  .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithHeader(CONTENTTYPE, APPLICATIONTYPE));
        }

        private static string GetJsonData(string filePath)
        {
            using var fileStream = new StreamReader(filePath);
            return fileStream.ReadToEnd();
        }
    }
}