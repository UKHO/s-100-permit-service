using FluentValidation.TestHelper;
using UKHO.S100PermitService.Common.Models.Request;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.UnitTests.Validations
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
            var userPermits = GetValidUserPermit();

            var result = _userPermitValidator.TestValidate(userPermits);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public void WhenInvalidUpnLength_ThenUpnLengthValidationErrorFound()
        {
            var result = _userPermitValidator.TestValidate(GeUserPermitWithInvalidUpnLength());

            result.ShouldHaveValidationErrorFor(x => x.Upn)
                .WithErrorMessage("Invalid UPN found for: Aqua Radar. UPN must be 46 characters long");
        }

        [Test]
        public void WhenInvalidChecksum_ThenChecksumValidationErrorFound()
        {
            var result = _userPermitValidator.TestValidate(GeUserPermitWithInvalidChecksum());

            result.ShouldHaveValidationErrorFor(x => x.Upn)
                .WithErrorMessage("Invalid checksum found for: Aqua Radar");
        }

        [Test]
        public void WhenInvalidCharactersFoundInTitle_ThenTitleValidationErrorFound()
        {
            var result = _userPermitValidator.TestValidate(GetUserPermitWithInvalidCharactersInTitle());

            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Invalid title found : SeaRadar X*");
        }

        [Test]
        public void WhenEmptyUpn_ThenUpnValidationErrorFound()
        {
            var result = _userPermitValidator.TestValidate(GeUserPermitWithEmptyUpn());
            
            result.ShouldHaveValidationErrorFor(x => x.Upn)
               .WithErrorMessage("UPN cannot be empty.");
        }

        private static UserPermit GetValidUserPermit()
        {
            return new UserPermit
            {
                Title = "Aqua Radar",
                Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF8J0K1L2"
            };
        }

        private static UserPermit GeUserPermitWithInvalidUpnLength()
        {
            return new UserPermit
            {
                Title = "Aqua Radar",
                Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF8J0K1L"
            };
        }

        private static UserPermit GeUserPermitWithInvalidChecksum()
        {
            return new UserPermit
            {
                Title = "Aqua Radar",
                Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF9J0K1L2"
            };
        }

        private static UserPermit GetUserPermitWithInvalidCharactersInTitle()
        {
            return new UserPermit
            {
                Title = "SeaRadar X*",
                Upn = "E9FAE304D230E4C729288349DA29776EE9B57E01M3N4O5"
            };
        }
        private static UserPermit GeUserPermitWithEmptyUpn()
        {
            return new UserPermit
            {
                Title = "Aqua Radar",
                Upn = string.Empty,
            };
        }
    }
}