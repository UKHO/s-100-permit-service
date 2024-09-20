using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Net;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;
using UKHO.S100PermitService.API.FunctionalTests.Factories;

namespace UKHO.S100PermitService.API.FunctionalTests.FunctionalTests
{
    public class IntegrationTests : TestBase
    {
        private PermitServiceApiConfiguration? _permitServiceApiConfiguration;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var serviceProvider = GetServiceProvider();
            _permitServiceApiConfiguration = serviceProvider!.GetRequiredService<IOptions<PermitServiceApiConfiguration>>().Value;
        }

        [Test]
        public async Task WhenICallPermitServiceEndpointForLicenceIdWhichDoesNotHaveUPN_ThenInternalServerError500IsReturned()
        {
            foreach(var licenceId in _permitServiceApiConfiguration!.InvalidUPNLicenceId!)
            {
                var response = await PermitServiceEndPointFactory.PermitServiceEndPoint(_permitServiceApiConfiguration!.BaseUrl, null, licenceId);
                response.StatusCode.Should().Be((HttpStatusCode)500);
            }
        }
    }     
}
