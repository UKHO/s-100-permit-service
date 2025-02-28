using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        private string? _authToken;
        private RequestBodyModel? _payload;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _authTokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
            _authToken = await _authTokenProvider!.GetPermitServiceTokenAsync(_tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration.ClientSecret!);
            _payload = await PermitServiceEndPointFactory.LoadPayloadAsync("./TestData/Payload/validPayload.json");
            _payload.products!.ForEach(p => p.permitExpiryDate = PermitXmlFactory.UpdateDate());
        }

        // PBI 201014 : Change GET method to POST method and the request model for Permits Endpoint - /v1/permits/s100
        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload!);
            response.StatusCode.Should().Be((HttpStatusCode)200);
            response.Headers.GetValues("Origin").Should().Contain("PermitService");
        }

        // PBI 201014 : Change GET method to POST method and the request model for Permits Endpoint - /v1/permits/s100
        // PBI 203832 : S-100 Permit Service Request and Response
        [Test]
        public async Task WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            var noAuthToken = await _authTokenProvider!.GetPermitServiceTokenAsync(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_permitServiceApiConfiguration!.BaseUrl, noAuthToken, _payload!);
            response.StatusCode.Should().Be((HttpStatusCode)403);
            response.Headers.GetValues("Origin").Should().Contain("PermitService");
        }

        // PBI 201014 : Change GET method to POST method and the request model for Permits Endpoint - /v1/permits/s100
        // PBI 203832 : S-100 Permit Service Request and Response
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorisedStatusCode401IsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_permitServiceApiConfiguration!.BaseUrl, _permitServiceApiConfiguration.InvalidToken, _payload!);
            response.StatusCode.Should().Be((HttpStatusCode)401);
            response.Headers.GetValues("Origin").Should().Contain("PermitService");
        }

        // PBI 203803 : S-100 Permit Service Validations
        // PBI 203832 : S-100 Permit Service Request and Response
        [Test]
        [TestCase("50ProductsPayload", "Permits", TestName = "WhenICallPermitServiceEndpointWith50ProductsAnd3UPNs_Then200OKResponseAndPERMITSZipIsReturned")]
        [TestCase("duplicateProductsPayload", "DuplicatePermits", TestName = "WhenICallPermitServiceEndpointWithDuplicateProducts_ThenProductWithHigherExpiryDateInPERMITIsReturned")]   
        public async Task WhenICallPermitServiceEndpointWithValidPayload_Then200OKResponseIsReturnedAlongWithPERMITSZip(string payload, string comparePermitFolderName)
        {
            _payload = await PermitServiceEndPointFactory.LoadPayloadAsync($"./TestData/Payload/{payload}.json");
            _payload.products!.ForEach(p => p.permitExpiryDate = PermitXmlFactory.UpdateDate());
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload);
            response.Headers.GetValues("Origin").Should().Contain("PermitService");
            var downloadPath = await PermitServiceEndPointFactory.DownloadZipFileAsync(response);
            PermitXmlFactory.VerifyPermitsZipStructureAndPermitXmlContents(downloadPath, _permitServiceApiConfiguration!.InvalidChars, _permitServiceApiConfiguration!.PermitHeaders!, _permitServiceApiConfiguration!.UserPermitNumbers!, comparePermitFolderName);
        }

        // PBI 203803 : S-100 Permit Service Validations
        [Test]
        public async Task WhenICallPermitServiceEndpointWithPayloadHavingPastDateAsExpiryDate_Then400BadRequestIsReturned()
        { 
           _payload = await PermitServiceEndPointFactory.LoadPayloadAsync($"./TestData/Payload/payloadWithPastExpiry.json");
           var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload);
           response.StatusCode.Should().Be((HttpStatusCode)400);
           response.Headers.GetValues("Origin").Should().Contain("PermitService");
        }

        // PBI 203832 : S-100 Permit Service Request and Response
        [Test]
        [TestCase("unauthorisedPKSRequest", 401, TestName = "WhenICallPermitServiceEndpointButRequestToPKSIsUnauthorised_Then401UnauthorisedIsReturned")]
        [TestCase("forbiddenPKSRequest", 403, TestName = "WhenICallPermitServiceEndpointButRequestToPKSIsForbidden_Then403ForbiddenIsReturned")]
        [TestCase("notFoundPKSRequest", 400, TestName = "WhenICallPermitServiceEndpointForProductNotAvailableInPKS_Then400BadRequestIsReturned")]
        public async Task WhenICallPermitServiceEndpointButSomeIssueInRequestToPKS_ThenExpectedPKSResponseIsReturnedWithOrigin(string payload, HttpStatusCode expectedStatusCode)
        {
            _payload = await PermitServiceEndPointFactory.LoadPayloadAsync($"./TestData/Payload/{payload}.json");
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload);
            response.StatusCode.Should().Be(expectedStatusCode);
            response.Headers.GetValues("Origin").Should().Contain("PKS");
        }

        // PBI 203803 : S-100 Permit Service Validations
        [Test]
        public async Task WhenICallPermitServiceEndPointWithInvalidUrl_Then404NotFoundIsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPointAsync(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload, false);
            response.StatusCode.Should().Be((HttpStatusCode)404);
        }

        [TearDown]
        public void TearDown()
        {
            //Clean up downloaded files/folders
            PermitXmlFactory.DeleteFolder(Path.Combine(Path.GetTempPath(), _permitServiceApiConfiguration!.TempFolderName!));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Cleanup();
        }
    }
}