﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Net;
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

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _authTokenProvider = new AuthTokenProvider();
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointWithInvalidLicenceId_ThenInternalServerError500IsReturned()
        {
            var authToken = await _authTokenProvider!.GetPermitServiceToken(_tokenConfiguration!.ClientIdWithAuth!, _tokenConfiguration.ClientSecret!);
            foreach (var licenceId in _permitServiceApiConfiguration!.InvalidLicenceIds!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, authToken, licenceId.ToString());
                response.StatusCode.Should().Be((HttpStatusCode)500);
            }  
        }
    }
}