using FluentAssertions;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.UnitTests.Validations
{
    [TestFixture]
    public class UserPermitServiceResponseValidatorTests
    {
        [Test]
        [TestCase(null)]
        [TestCase("empty")]
        public void WhenUserPermitServiceResponseIsNull_ThenReturnsTrue(string? obj)
        {
            var userPermitServiceResponse = obj is null ? null : new UserPermitServiceResponse();

            var result = UserPermitServiceResponseValidator.IsResponseNull(userPermitServiceResponse);

            result.Should().BeTrue();
        }

        [Test]
        public void WhenUserPermitServiceResponseIsNotNull_ThenReturnsFalse()
        {
            List<UserPermit> userPermits = [new UserPermit { Title = "Title", Upn = "Upn" }];
            var userPermitServiceResponse = new UserPermitServiceResponse { LicenceId = 1, UserPermits = userPermits };

            var result = UserPermitServiceResponseValidator.IsResponseNull(userPermitServiceResponse);

            result.Should().BeFalse();
        }
    }
}
