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
    public class IntegrationTests : TestBase
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

        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoesNotHaveHoldings_ThenInternalServerError500IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.InvalidHoldingsLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)500);
            }
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoesNotHaveUPN_ThenInternalServerError500IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.InvalidUPNLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)500);
            }
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoesNotHaveKey_ThenInternalServerError500IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.InvalidPKSLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, _authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)500);
            }
        }
    }
}