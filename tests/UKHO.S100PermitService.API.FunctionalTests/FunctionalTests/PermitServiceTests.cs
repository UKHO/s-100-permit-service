using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.S100PermitService.API.FunctionalTests.Auth;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;
using UKHO.S100PermitService.API.FunctionalTests.Factories;
using static UKHO.S100PermitService.API.FunctionalTests.Models.S100PermitServiceRequestModel;

namespace UKHO.S100PermitService.API.FunctionalTests.FunctionalTests
{
    public class PermitServiceTests : TestBase
    {
        private AuthTokenProvider? _authTokenProvider;
        private TokenConfiguration? _tokenConfiguration;
        private PermitServiceApiConfiguration? _permitServiceApiConfiguration;
        private DataKeyVaultConfiguration? _dataKeyVaultConfiguration;
        private KeyVaultConfiguration? _keyVaultConfiguration;
        private string? _authToken;
        private RequestBodyModel? _payload;

        private ILoggerFactory? _loggerFactory;
        private ILogger _logger;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddSimpleConsole(o =>
                    {
                        o.SingleLine = true;
                        o.TimestampFormat = "HH:mm:ss ";
                    })
                    .SetMinimumLevel(LogLevel.Information);
            });
            _logger = _loggerFactory.CreateLogger<PermitServiceTests>();

            // Pass logger to the endpoint factory
            PermitServiceEndPointFactory.SetLogger(_loggerFactory.CreateLogger("PermitServiceEndPointFactory"));
            _logger.LogInformation("Functional test OneTimeSetup starting.");

            _authTokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
            _dataKeyVaultConfiguration = serviceProvider?.GetRequiredService<IOptions<DataKeyVaultConfiguration>>().Value;
            _keyVaultConfiguration = serviceProvider?.GetRequiredService<IOptions<KeyVaultConfiguration>>().Value;

            _authToken = await _authTokenProvider!.GetPermitServiceTokenAsync(_tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration.ClientSecret!);
            _payload = await PermitServiceEndPointFactory.LoadPayloadAsync("./TestData/Payload/validPayload.json");
            _payload.products!.ForEach(p => p.permitExpiryDate = PermitXmlFactory.UpdateDate());

            PermitServiceEndPointFactory.InitializeHttpClient(_permitServiceApiConfiguration!.BaseUrl!);

            _logger.LogInformation("Functional test OneTimeSetup completed. BaseUrl={BaseUrl}", _permitServiceApiConfiguration.BaseUrl);
        }

        // PBI 201014 : Change GET method to POST method and the request model for Permits Endpoint - /v1/permits/s100
        // PBI 206666 : Adding Origin in Response Header for 200 status code
        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            _logger.LogInformation("START {TestName}", nameof(WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned));
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_authToken, _payload!);
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response {Status} | Origin:{Origin} | BodySnippet:{Body}", (int)response.StatusCode, GetOrigin(response), Truncate(body));
            using(Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(response.Headers.GetValues("Origin"), Does.Contain("PermitService"));
            }
        }

        // PBI 201014 : Change GET method to POST method and the request model for Permits Endpoint - /v1/permits/s100
        // PBI 203832 : S-100 Permit Service Request and Response
        // PBI 206666 : Adding Origin in Response Header for 200 status code
        [Test]
        public async Task WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            _logger!.LogInformation("START {TestName}", nameof(WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned));
            var noAuthToken = await _authTokenProvider!.GetPermitServiceTokenAsync(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(noAuthToken, _payload!);
            _logger.LogInformation("Response {Status} | Origin:{Origin}", (int)response.StatusCode, GetOrigin(response));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(response.Headers.GetValues("Origin"), Does.Contain("PermitService"));
        }

        // PBI 201014 : Change GET method to POST method and the request model for Permits Endpoint - /v1/permits/s100
        // PBI 203832 : S-100 Permit Service Request and Response
        // PBI 206666 : Adding Origin in Response Header for 200 status code
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorisedStatusCode401IsReturned()
        {
            _logger!.LogInformation("START {TestName}", nameof(WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorisedStatusCode401IsReturned));
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_permitServiceApiConfiguration!.InvalidToken, _payload!);
            _logger.LogInformation("Response {Status} | Origin:{Origin}", (int)response.StatusCode, GetOrigin(response));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.GetValues("Origin"), Does.Contain("PermitService"));
        }

        // PBI 203803 : S-100 Permit Service Validations
        // PBI 203832 : S-100 Permit Service Request and Response
        // PBI 206666 : Adding Origin in Response Header for 200 status code
        // PBI 220259 : Store the Permit.Sign File in a ZIP
        [Test]
        [TestCase("50ProductsPayload", "Permits", TestName = "WhenICallPermitServiceEndpointWith50ProductsAnd3UPNs_Then200OKResponseAndPERMITSZipIsReturned")]
        [TestCase("duplicateProductsPayload", "DuplicatePermits", TestName = "WhenICallPermitServiceEndpointWithDuplicateProducts_ThenProductWithHigherExpiryDateInPERMITIsReturned")]   
        public async Task WhenICallPermitServiceEndpointWithValidPayload_Then200OKResponseIsReturnedAlongWithPERMITSZip(string payload, string comparePermitFolderName)
        {
            _logger!.LogInformation("START {TestName} | Payload={Payload}", nameof(WhenICallPermitServiceEndpointWithValidPayload_Then200OKResponseIsReturnedAlongWithPERMITSZip), payload);
            _payload = await PermitServiceEndPointFactory.LoadPayloadAsync($"./TestData/Payload/{payload}.json");
            _payload.products!.ForEach(p => p.permitExpiryDate = PermitXmlFactory.UpdateDate());
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_authToken, _payload);
            _logger.LogInformation("Response {Status} | Origin:{Origin}", (int)response.StatusCode, GetOrigin(response));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers.GetValues("Origin"), Does.Contain("PermitService"));
            var downloadPath = await PermitServiceEndPointFactory.DownloadZipFileAsync(response);
            _logger.LogInformation("ZIP extracted at {Path}", downloadPath);
            PermitXmlFactory.VerifyPermitsZipStructureAndPermitXmlContents(downloadPath, _permitServiceApiConfiguration!.InvalidChars, _permitServiceApiConfiguration!.PermitHeaders!, _permitServiceApiConfiguration!.UserPermitNumbers!, comparePermitFolderName);
            var isSignatureValid = await PermitXmlFactory.VerifySignatureTask(downloadPath, _keyVaultConfiguration!.ServiceUri!, _dataKeyVaultConfiguration!.DsCertificate!, _tokenConfiguration!.TenantId!, _tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration!.ClientSecret!);
            _logger.LogInformation("Signature Valid={Valid}", isSignatureValid);
            Assert.That(isSignatureValid);
        }

        // PBI 203803 : S-100 Permit Service Validations
        // PBI 206666 : Adding Origin in Response Header for 200 status code
        [Test]
        public async Task WhenICallPermitServiceEndpointWithPayloadHavingPastDateAsExpiryDate_Then400BadRequestIsReturned()
        {
            _logger!.LogInformation("START {TestName}", nameof(WhenICallPermitServiceEndpointWithPayloadHavingPastDateAsExpiryDate_Then400BadRequestIsReturned));
            _payload = await PermitServiceEndPointFactory.LoadPayloadAsync("./TestData/Payload/payloadWithPastExpiry.json");
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_authToken, _payload);
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response {Status} | Origin:{Origin} | BodySnippet:{Body}", (int)response.StatusCode, GetOrigin(response), Truncate(body));
            using(Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(response.Headers.GetValues("Origin"), Does.Contain("PermitService"));
            }
        }

        // PBI 203832 : S-100 Permit Service Request and Response
        [Test]
        [TestCase("unauthorisedPKSRequest", 401, TestName = "WhenICallPermitServiceEndpointButRequestToPKSIsUnauthorised_Then401UnauthorisedIsReturned")]
        [TestCase("forbiddenPKSRequest", 403, TestName = "WhenICallPermitServiceEndpointButRequestToPKSIsForbidden_Then403ForbiddenIsReturned")]
        [TestCase("notFoundPKSRequest", 400, TestName = "WhenICallPermitServiceEndpointForProductNotAvailableInPKS_Then400BadRequestIsReturned")]
        public async Task WhenICallPermitServiceEndpointButSomeIssueInRequestToPKS_ThenExpectedPKSResponseIsReturnedWithOrigin(string payload, HttpStatusCode expectedStatusCode)
        {
            _logger!.LogInformation("START {TestName} | Payload={Payload}", nameof(WhenICallPermitServiceEndpointButSomeIssueInRequestToPKS_ThenExpectedPKSResponseIsReturnedWithOrigin), payload);
            _payload = await PermitServiceEndPointFactory.LoadPayloadAsync($"./TestData/Payload/{payload}.json");
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_authToken, _payload);
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response {Status} | Origin:{Origin} | BodySnippet:{Body}", (int)response.StatusCode, GetOrigin(response), Truncate(body));
            using(Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(expectedStatusCode), body);
                Assert.That(response.Headers.GetValues("Origin"), Does.Contain("PKS"));
            }
        }

        // PBI 203803 : S-100 Permit Service Validations
        [Test]
        public async Task WhenICallPermitServiceEndPointWithInvalidUrl_Then404NotFoundIsReturned()
        {
            _logger!.LogInformation("START {TestName}", nameof(WhenICallPermitServiceEndPointWithInvalidUrl_Then404NotFoundIsReturned));
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_authToken, _payload!, false);
            _logger.LogInformation("Response {Status}", (int)response.StatusCode);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                PermitXmlFactory.DeleteFolder(Path.Combine(Path.GetTempPath(), _permitServiceApiConfiguration!.TempFolderName!));
                _logger?.LogInformation("TearDown cleanup complete.");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Cleanup failed.");
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _logger?.LogInformation("Functional tests OneTimeTearDown executing.");
            _loggerFactory?.Dispose();
            Cleanup();
        }

        private static string GetOrigin(HttpResponseMessage response) =>
            response.Headers.TryGetValues("Origin", out var values)
                ? string.Join(",", values)
                : "(none)";

        private static string Truncate(string? s)
        {
            if(string.IsNullOrWhiteSpace(s))
            {
                return "(empty)";
            }

            return s.Length <= 250 ? s : s[..250] + "...(truncated)";
        }
    }
}