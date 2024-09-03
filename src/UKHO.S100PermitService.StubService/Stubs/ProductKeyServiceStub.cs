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
        private readonly ProductKeyServiceConfiguration productKeyServiceConfiguration;

        public ProductKeyServiceStub(ProductKeyServiceConfiguration productKeyServiceConfiguration)
        {
            this.productKeyServiceConfiguration = productKeyServiceConfiguration;
        }

        public void ConfigureStub(WireMockServer server)
        {
            server //401
             .Given(Request.Create()
             .WithPath(productKeyServiceConfiguration.Url)
             .UsingPost()
             .WithHeader("Authorization", "Bearer ", MatchBehaviour.RejectOnMatch))
             .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                .WithBody(@"{ ""result"": ""token missing""}"));

            server //404
                  .Given(Request.Create()
                  .WithPath(productKeyServiceConfiguration.Url)
                  .UsingPost()
                  .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                  .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.NotFound)
                        .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                        .WithBody("{\r\n  \"correlationId\": \"4c2dec2a-f44f-445d-837f-99fa72836533\",\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetProductKey\",\r\n      \"description\": \"Key not found for Product and Edition \"\r\n    }\r\n  ]\r\n}\r\n"));

            server //404
                  .Given(Request.Create()
                  .WithPath(productKeyServiceConfiguration.Url)
                  .UsingPost()
                  .WithBody(new JsonMatcher(GetJsonData(Path.Combine("StubData\\PKS", "request-404.json"))))
                  .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                  .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.NotFound)
                        .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                        .WithBodyFromFile(Path.Combine("StubData\\PKS", "response-404.json")));

            server //200
                  .Given(Request.Create()
                  .WithPath(productKeyServiceConfiguration.Url)
                  .UsingPost()
                  .WithBody(new JsonMatcher(GetJsonData(Path.Combine("StubData\\PKS", "request-200.json"))))
                  .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
                  .RespondWith(Response.Create()
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                        .WithBodyFromFile(Path.Combine("StubData\\PKS", "response-200.json")));

            server //400
                  .Given(Request.Create()
                  .WithPath(productKeyServiceConfiguration.Url)
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