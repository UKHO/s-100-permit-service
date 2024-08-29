using Microsoft.Extensions.Logging;
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
        private readonly ProductKeyServiceConfiguration _productKeyServiceConfiguration;
        private readonly ILogger<ProductKeyServiceStub> _logger;

        private const string STUBSFOLDERPATH = @"_files\ProductKeyServiceStub";
        public const string CONTENTTYPE = "Content-Type";
        public const string APPLICATIONTYPE = "application/json";

        public ProductKeyServiceStub(ProductKeyServiceConfiguration productKeyServiceConfiguration, ILogger<ProductKeyServiceStub> logger)
        {
            _productKeyServiceConfiguration = productKeyServiceConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ConfigureStub(WireMockServer server)
        {
            _logger.LogInformation("Configuring ProductKeyServiceStub with URL: {Url}", _productKeyServiceConfiguration.Url);

            if(!Directory.Exists(STUBSFOLDERPATH))
            {
                throw new DirectoryNotFoundException($"The specified stubs folder path does not exist: {STUBSFOLDERPATH}");
            }

            server.Given(Request.Create().WithPath(_productKeyServiceConfiguration.Url)
                    .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), STUBSFOLDERPATH, "request1.json"))))
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader(CONTENTTYPE, APPLICATIONTYPE)
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), STUBSFOLDERPATH, "response1.json")));

            server
                .Given(Request.Create().WithPath(_productKeyServiceConfiguration.Url).UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{ \"message\": \"PKS API Stub response\" }"));

            _logger.LogInformation("ProductKeyServiceStub configured.");
        }

        private static string GetJsonData(string filePath)
        {
            using var fileStream = new StreamReader(filePath);
            return fileStream.ReadToEnd();
        }
    }
}