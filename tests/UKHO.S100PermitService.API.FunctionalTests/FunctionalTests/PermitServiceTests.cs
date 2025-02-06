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
        private string _payload = "dummy";

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _authTokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
            _authToken = await _authTokenProvider!.AsyncGetPermitServiceToken(_tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration.ClientSecret!);
        }

        [Ignore("Temporarily ignoring the functional tests till all the respective changes of S100 Permit Service are done")]
        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _payload);
            response.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Ignore("Temporarily ignoring the functional tests till all the respective changes of S100 Permit Service are done")]
        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            var noAuthToken = await _authTokenProvider!.AsyncGetPermitServiceToken(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, noAuthToken, _payload);
            response.StatusCode.Should().Be((HttpStatusCode)403);
        }

        [Ignore("Temporarily ignoring the functional tests till all the respective changes of S100 Permit Service are done")]
        // PBI 172720: Add AD Auth to get permits EndPoint
        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorizedStatusCode401IsReturned()
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _permitServiceApiConfiguration.InvalidToken, _payload);
            response.StatusCode.Should().Be((HttpStatusCode)401);
        }

        [Ignore("Temporarily ignoring the functional tests till all the respective changes of S100 Permit Service are done")]
        // PBI 172917: Build ZipStream as Response
        // PBI 172914: Remove duplicate dataset files & select the dataset file with highest expiry date
        [Test]
        [TestCase("payload", "Permits", TestName = "RenameTest1")]
        [TestCase("payload", "DuplicatePermits", TestName = "RenameTest2")]
        public async Task WhenICallPermitServiceEndpointWithValidPayload_Then200OKResponseIsReturnedAlongWithPERMITSZip(string payload, string comparePermitFolderName)
        {
            var response = await PermitServiceEndPointFactory.AsyncPermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, payload);
            var downloadPath = await PermitServiceEndPointFactory.AsyncDownloadZipFile(response);
            PermitXmlFactory.VerifyPermitsZipStructureAndPermitXmlContents(downloadPath, _permitServiceApiConfiguration!.InvalidChars, _permitServiceApiConfiguration!.PermitHeaders!, _permitServiceApiConfiguration!.UserPermitNumbers!, comparePermitFolderName);
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