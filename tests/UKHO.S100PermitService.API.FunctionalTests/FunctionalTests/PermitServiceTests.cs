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
        private AuthTokenProvider? _tokenProvider;
        private TokenConfiguration? _tokenConfiguration;
        private PermitServiceApiConfiguration? _psApiConfiguration;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _tokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _psApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            var token = await _tokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientId!, _tokenConfiguration.ClientSecret!);
            var response = await PermitServiceEndPointHelper.PermitServiceEndPoint(_psApiConfiguration!.BaseUrl, token, _psApiConfiguration.ValidLicenceId);
            response.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithNoRequiredRoleToken_ThenForbiddenStatusCode403IsReturned()
        {
            var token = await _tokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientIdNoAuth!, _tokenConfiguration.ClientSecretNoAuth!);
            var response = await PermitServiceEndPointHelper.PermitServiceEndPoint(_psApiConfiguration!.BaseUrl, token, _psApiConfiguration.ValidLicenceId);
            response.StatusCode.Should().Be((HttpStatusCode)403);
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithInValidToken_ThenUnauthorizedStatusCode401IsReturned()
        {
            var response = await PermitServiceEndPointHelper.PermitServiceEndPoint(_psApiConfiguration!.BaseUrl, _psApiConfiguration.InvalidToken, _psApiConfiguration.ValidLicenceId);
            response.StatusCode.Should().Be((HttpStatusCode)401);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Cleanup();
        }
    }
}