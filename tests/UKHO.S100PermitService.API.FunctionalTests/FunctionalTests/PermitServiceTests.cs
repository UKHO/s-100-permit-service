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
            _authToken = await _authTokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration.ClientSecret!);
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)200);
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            var noAuthToken = await _authTokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, noAuthToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)403);
        }

        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorizedStatusCode401IsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _permitServiceApiConfiguration.InvalidToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)401);
        }

        // PBI 172721: Get Holdings from Shop Facade stub
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInvalidLicenceIdAsAlphanumericSpecialChars_ThenBadRequest400IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.NonIntegerLicenceIds!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId);
                response.StatusCode.Should().Be((HttpStatusCode)400);
            }
        }

        // PBI 172721: Get Holdings from Shop Facade stub
        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoesNotHaveHoldings_ThenInternalServerError500IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.InvalidHoldingsLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)500);
            }
        }

        // PBI 172722: Get UPNs from Shop Facade stub
        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoesNotHaveUPN_ThenInternalServerError500IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.InvalidUPNLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)500);
            }
        }

        // PBI 172910: Get Permit Keys from PKS stub
        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoesNotHaveKey_ThenInternalServerError500IsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.InvalidPKSLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)500);
        }

        // PBI 179438: Product Backlog Item 179438: Handle successful request with empty response for Get UPNs and Get Holdings
        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoNotHaveDataAvailable_ThenNoContent204IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.NoDataLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)204);
            }
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichHaveMutipleCellsInHoldings_Then200OKResponseIsReturnedAlongWithPERMITXML()
        {  
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            var downloadPath = await PermitServiceEndPointFactory.DownloadZipFile(response);
            PermitXmlFactory.VerifyPermitsZipStructureAndContents(downloadPath.ToString()!, _permitServiceApiConfiguration!.InvalidChars, _permitServiceApiConfiguration!.PermitHeaders!, _permitServiceApiConfiguration!.UPNs!);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichHaveInvalidValueOfExpiryDate_ThenInternalServerError500IsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.InvalidExpiryDateLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)500);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichHaveDuplicateCellsInHoldings_Then200OKResponseIsReturnedAlongWithPERMITXML()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.DuplicateHoldingsLicenceId.ToString()!);
            var downloadPath = await PermitServiceEndPointFactory.DownloadZipFile(response);
            PermitXmlFactory.VerifyPermitsZipStructureAndContents(downloadPath.ToString()!, _permitServiceApiConfiguration!.InvalidChars, _permitServiceApiConfiguration!.PermitHeaders!, _permitServiceApiConfiguration!.UPNs!, "DuplicatePermits");
        }

        [TearDown]
        public void TearDown()
        {
            //Clean up downloaded files/folders
            PermitXmlFactory.DeleteFolder(Path.Combine(Path.GetTempPath(), "temp"));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Cleanup();
        }
    }
}