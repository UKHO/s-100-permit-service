using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Net;
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
        private string _authToken;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _authTokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
            _authToken = await _authTokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration.ClientSecret!);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            var noAuthToken = await _authTokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, noAuthToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)403);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorizedStatusCode401IsReturned()
        {
            var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _permitServiceApiConfiguration.InvalidToken, _permitServiceApiConfiguration.ValidLicenceId.ToString()!);
            response.StatusCode.Should().Be((HttpStatusCode)401);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithInvalidLicenceIdAsAlphanumericSpecialChars_ThenBadRequest400IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.NonIntegerLicenceIds!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId);
                response.StatusCode.Should().Be((HttpStatusCode)400);
            }
        }

            [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Cleanup();
        }
    }
}