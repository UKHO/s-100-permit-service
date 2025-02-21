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
        private RequestBodyModel _payload;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _authTokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
            _authToken = await _authTokenProvider!.AsyncGetPermitServiceToken(_tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration.ClientSecret!);
            _payload = await PermitServiceEndPointFactory.LoadPayload("./TestData/Payload/validPayload.json");
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload!);
            response.StatusCode.Should().Be((HttpStatusCode)200);
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            var noAuthToken = await _authTokenProvider!.AsyncGetPermitServiceToken(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, noAuthToken, _payload!);
            response.StatusCode.Should().Be((HttpStatusCode)403);
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorizedStatusCode401IsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _permitServiceApiConfiguration.InvalidToken, _payload!);
            response.StatusCode.Should().Be((HttpStatusCode)401);
        }

        // PBI 172917: Build ZipStream as Response
        // PBI 172914: Remove duplicate dataset files & select the dataset file with highest expiry date
        [Test]
        [TestCase("50ProductsPayload", "Permits", TestName = "WhenICallPermitServiceEndpointWith50ProductsAnd3UPNs_Then200OKResponseIsReturnedAndPERMITSZipIsGeneratedSuccessfully")]
        //[TestCase("duplicateProductsPayload", "DuplicatePermits", TestName = "RenameTest2")]    // This test case will work post the merge of PBi 203803
        public async Task WhenICallPermitServiceEndpointWithValidPayload_Then200OKResponseIsReturnedAlongWithPERMITSZip(string payload, string comparePermitFolderName)
        {
            _payload = await PermitServiceEndPointFactory.LoadPayload($"./TestData/Payload/{payload}.json");
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload);
            var downloadPath = await PermitServiceEndPointFactory.AsyncDownloadZipFile(response);
            PermitXmlFactory.VerifyPermitsZipStructureAndPermitXmlContents(downloadPath, _permitServiceApiConfiguration!.InvalidChars, _permitServiceApiConfiguration!.PermitHeaders!, _permitServiceApiConfiguration!.UserPermitNumbers!, comparePermitFolderName);
        }

        [Test]
        [TestCase("unauthroizedPKSRequest",401, TestName = "WhenICallPermitServiceEndpointButPermitServiceRequestIsUnauthroized_Then401UnauthorizedIsReturned")]
        [TestCase("forbiddenPKSRequest", 403, TestName = "WhenICallPermitServiceEndpointButPermitServiceIsForbidden_Then403ForbiddenIsReturned")]
        [TestCase("notFoundPKSRequest", 400, TestName = "WhenICallPermitServiceEndpointForProductNotAvailableInPKS_Then400VadRequestIsReturned")]
        public async Task WhenICallPermitServiceEndpointWhichHasLimitedOrNoAccessToPKS_ThenPKSResponseIsReturnedByPermitService(string payload, HttpStatusCode expectedStatusCode)
        {
            _payload = await PermitServiceEndPointFactory.LoadPayload($"./TestData/Payload/{payload}.json");
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload);
            response.StatusCode.Should().Be(expectedStatusCode);
            response.Headers.GetValues("Origin").Should().Contain("PKS");
        }

        [Test]
        public async Task WhenICallPermitServiceEndPointWithInvalidUrl_Then404NotFoundIsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload, false);
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