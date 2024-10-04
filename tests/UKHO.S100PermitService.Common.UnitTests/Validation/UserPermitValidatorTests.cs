using FluentAssertions;
using FluentValidation.TestHelper;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Validation;

namespace UKHO.S100PermitService.Common.UnitTests.Validation
{
    [TestFixture]
    public class UserPermitValidatorTests
    {
        private UserPermitValidator _userPermitValidator;

        [SetUp]
        public void Setup()
        {
            _userPermitValidator = new UserPermitValidator();
        }

        [Test]
        public void WhenUserPermitsWithValidUpns_ThenNoValidationErrorsAreFound()
        {
            var userPermitServiceResponse = GetValidUserPermitServiceResponse();

            var result = _userPermitValidator.TestValidate(userPermitServiceResponse);

            result.Errors.Count.Should().Be(0);
        }

        [Test]
        public void WhenInvalidUpnLength_ThenUpnLengthValidationErrorFound()
        {
            var result = _userPermitValidator.TestValidate(GeUserPermitServiceResponseWithInvalidUpnLength());

            result.Errors.Count.Should().Be(2);
            result.ShouldHaveAnyValidationError().WithErrorMessage("Invalid UPN. UPN must be 46 characters long");
        }

        [Test]
        public void WhenInvalidChecksum_ThenChecksumValidationErrorFound()
        {
            var result = _userPermitValidator.TestValidate(GeUserPermitServiceResponseWithInvalidChecksum());

            result.Errors.Count.Should().Be(3);
            result.ShouldHaveAnyValidationError().WithErrorMessage("Invalid checksum");
        }

        private static UserPermitServiceResponse GetValidUserPermitServiceResponse()
        {
            return new UserPermitServiceResponse()
            {
                LicenceId = 1,
                UserPermits = [ new UserPermit{ Title = "Aqua Radar", Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF8J0K1L2" },
                    new UserPermit{  Title= "SeaRadar X", Upn = "E9FAE304D230E4C729288349DA29776EE9B57E01M3N4O5" },
                    new UserPermit{ Title = "Navi Radar", Upn = "F1EB202BDC150506E21E3E44FD1829424462D958P6Q7R8" }
                ]
            };
        }

        private static UserPermitServiceResponse GeUserPermitServiceResponseWithInvalidUpnLength()
        {
            return new UserPermitServiceResponse()
            {
                LicenceId = 1,
                UserPermits = [ new UserPermit{ Title = "Aqua Radar", Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF8J0K1L" },
                    new UserPermit{  Title= "SeaRadar X", Upn = "E9FAE304D230E4C729288349DA29776EE9B57E01M3N4O" },
                    new UserPermit{ Title = "Navi Radar", Upn = "F1EB202BDC150506E21E3E44FD1829424462D958P6Q7R8" }
                ]
            };
        }

        private static UserPermitServiceResponse GeUserPermitServiceResponseWithInvalidChecksum()
        {
            return new UserPermitServiceResponse()
            {
                LicenceId = 1,
                UserPermits = [ new UserPermit{ Title = "Aqua Radar", Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF9J0K1L2" },
                    new UserPermit{  Title= "SeaRadar X", Upn = "E9FAE304D230E4C729288349DA29776EE9B57E02M3N4O5" },
                    new UserPermit{ Title = "Navi Radar", Upn = "F1EB202BDC150506E21E3E44FD1829424462D959P6Q7R8" }
                ]
            };
        }
    }
}
