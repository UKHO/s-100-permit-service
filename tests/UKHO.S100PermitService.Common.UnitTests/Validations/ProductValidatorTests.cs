using FluentAssertions;
using FluentValidation.TestHelper;
using UKHO.S100PermitService.Common.Models.Request;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.UnitTests.Validations
{
    [TestFixture]
    public class ProductValidatorTests
    {
        private readonly ProductValidator _validator;

        public ProductValidatorTests()
        {
            _validator = new ProductValidator();
        }

        [Test]
        public void WhenProductNameIsEmpty_ThenValidationErrorIsReturned()
        {
            var product = new Product
            {
                ProductName = string.Empty,
                EditionNumber = 1,
                PermitExpiryDate = "2022-12-31"
            };

            var result = _validator.TestValidate(product);

            result.ShouldHaveValidationErrorFor(p => p.ProductName)
                .WithErrorMessage("ProductName cannot be empty.");
        }

        [Test]
        public void WhenProductNameExceedsMaximumLength_ThenProductNameLengthValidationErrorIsReturned()
        {
            var product = new Product { ProductName = new string('A', 256) };

            var result = _validator.TestValidate(product);

            result.ShouldHaveValidationErrorFor(p => p.ProductName)
                .WithErrorMessage("Must be a maximum of 255 characters.");
        }

        [Test]
        public void WhenEditionNumberIsNotANaturalNumber_ThenEditionNumberValidationErrorIsReturned()
        {
            var product = new Product { EditionNumber = -1 };

            var result = _validator.TestValidate(product);

            result.ShouldHaveValidationErrorFor(p => p.EditionNumber)
                .WithErrorMessage("Must be a natural number i.e. positive number greater than 0.");
        }

        [Test]
        public void WhenPermitExpiryDateIsEmpty_ThenEmptyExpiryDateValidationErrorIsReturned()
        {
            var product = new Product { PermitExpiryDate = string.Empty };

            var result = _validator.TestValidate(product);

            result.ShouldHaveValidationErrorFor(p => p.PermitExpiryDate)
                .WithErrorMessage("PermitExpiryDate cannot be empty.");
        }

        [Test]
        public void WhenPermitExpiryDateIsNotInUTCFormat_ThenDateFormatValidationErrorIsReturned()
        {
            var product = new Product { PermitExpiryDate = "2022/12/01" };

            var result = _validator.TestValidate(product);

            result.ShouldHaveValidationErrorFor(p => p.PermitExpiryDate)
                .WithErrorMessage("Must be in UTC format YYYY-MM-DD.");
        }

        [Test]
        public void WhenPermitExpiryDateIsNotAValidDate_ThenValidDateValidationErrorIsReturned()
        {
            var product = new Product { PermitExpiryDate = "2022-02-30" };

            var result = _validator.TestValidate(product);

            result.ShouldHaveValidationErrorFor(p => p.PermitExpiryDate)
                .WithErrorMessage("Must be a valid date.");
        }

        [Test]
        public void WhenPermitExpiryDateIsNotBeTodayOrLater_ThenValidDateValidationErrorIsReturned()
        {
            var product = new Product { PermitExpiryDate = "2022-02-20" };

            var result = _validator.TestValidate(product);

            result.ShouldHaveValidationErrorFor(p => p.PermitExpiryDate)
                .WithErrorMessage("Must be today or a future date.");
        }

        [Test]
        public void WhenProductIsValid_ThenValidationErrorAreNotReturned()
        {
            var product = new Product
            {
                ProductName = "Test Product",
                EditionNumber = 1,
                PermitExpiryDate = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd")
            };

            var result = _validator.TestValidate(product);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }
}
