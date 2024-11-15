using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.S100PermitService.API.FunctionalTests.Auth;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;
using UKHO.S100PermitService.API.FunctionalTests.Factories;

namespace UKHO.S100PermitService.API.FunctionalTests.FunctionalTests
{
    public class PermitServiceTests : TestBase
    {
        private AuthTokenProvider? _authTokenProvider;
        private TokenConfiguration? _tokenConfiguration;
        private PermitServiceApiConfiguration? _permitServiceApiConfiguration;
        private string? _authToken;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _authTokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
            _authToken = await _authTokenProvider!.AsyncGetPermitServiceToken(_tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration.ClientSecret!);
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)200);
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            var noAuthToken = await _authTokenProvider!.AsyncGetPermitServiceToken(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, noAuthToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)403);
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorizedStatusCode401IsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _permitServiceApiConfiguration.InvalidToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)401);
        }

        // PBI 172721: Get Holdings from Shop Facade stub
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInvalidLicenceIdAsAlphanumericSpecialChars_ThenBadRequest400IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.NonIntegerLicenceIds!)
            {
                var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId);
                response.StatusCode.Should().Be((HttpStatusCode)400);
            }
        }

        // PBI 172721: Get Holdings from Shop Facade stub
        // PBI 172722: Get UPNs from Shop Facade stub
        // PBI 181294: Return 404 instead of 500 for Get Permits Endpoint when licence not found for Get UPNs and Get Holdings
        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWithMissingData_ThenNotFound404IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.MissingDataLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)404);
            }
        }

        // PBI 172722: Get UPNs from Shop Facade stub
        // PBI 172721: Get Holdings from Shop Facade stub
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInvalidLicenceId_ThenInternalServerError500IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.InvalidLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.InvalidLicenceId.ToString()!);
                response.StatusCode.Should().Be((HttpStatusCode)400);
            }

        }

        // PBI 172910: Get Permit Keys from PKS stub
        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoesNotHaveKey_ThenInternalServerError500IsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.InvalidPksLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)500);
        }

        // PBI 179438: Product Backlog Item 179438: Handle successful request with empty response for Get UPNs and Get Holdings
        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoNotHaveDataAvailable_ThenNoContent204IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.NoDataLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)204);
            }
        }

        // PBI 172917: Build ZipStream as Response
        // PBI 172914: Remove duplicate dataset files & select the dataset file with highest expiry date
        [Test]
        [TestCase("50", "Permits", TestName = "WhenICallPermitServiceEndpointForLicenceIdWhichHave50CellsInHoldings_Then200OKResponseIsReturnedAndPERMITSZipIsGeneratedSuccessfully")]
        [TestCase("12", "DuplicatePermits", TestName = "WhenICallPermitServiceEndpointForLicenceIdWhichHaveDuplicateCellsInHoldings_Then200OKResponseIsReturnedAndPERMITXmlIsGeneratedSuccessfullyWithHighestExpiryDate")]
        public async Task WhenICallPermitServiceEndpointWithLicenceId_Then200OKResponseIsReturnedAlongWithPERMITSZip(string licenceId, string comparePermitFolderName)
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId);
            var downloadPath = await PermitServiceEndPointFactory.AsyncDownloadZipFile(response);
            PermitXmlFactory.VerifyPermitsZipStructureAndPermitXmlContents(downloadPath, _permitServiceApiConfiguration!.InvalidChars, _permitServiceApiConfiguration!.PermitHeaders!, _permitServiceApiConfiguration!.UserPermitNumbers!, comparePermitFolderName);
        }

        // PBI 172917: Build ZipStream as Response
        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichHaveInvalidValueOfExpiryDate_ThenInternalServerError500IsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.InvalidExpiryDateLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)500);
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