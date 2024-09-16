using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Net;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;
using UKHO.S100PermitService.API.FunctionalTests.Helpers;

namespace UKHO.S100PermitService.API.FunctionalTests.FunctionalTests
{
    public class PermitServiceTests : TestBase
    {
        private AuthTokenProvider? _authTokenProvider;
        private TokenConfiguration? _tokenConfiguration;
        private PermitServiceApiConfiguration? _permitServiceApiConfiguration;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _authTokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            var token = await _authTokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientId!, _tokenConfiguration.ClientSecret!);
            var response = await PermitServiceEndPointHelper.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, token, _permitServiceApiConfiguration.ValidLicenceId);
            response.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithoutRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            var token = await _authTokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointHelper.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, token, _permitServiceApiConfiguration.ValidLicenceId);
            response.StatusCode.Should().Be((HttpStatusCode)403);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorizedStatusCode401IsReturned()
        {
            var response = await PermitServiceEndPointHelper.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _permitServiceApiConfiguration.InvalidToken, _permitServiceApiConfiguration.ValidLicenceId);
            response.StatusCode.Should().Be((HttpStatusCode)401);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Cleanup();
        }
    }
}