using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using UKHO.S100PermitService.Common.Models.Request;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.UnitTests.Validations
{
    [TestFixture]
    public class PermitRequestValidatorTests
    {
        private PermitRequestValidator permitRequestValidator;
        private IProductValidator productValidator;
        private IUserPermitValidator userPermitValidator;

        [SetUp]
        public void SetUp()
        {
            productValidator = A.Fake<IProductValidator>();
            userPermitValidator = A.Fake<IUserPermitValidator>();
            permitRequestValidator = new PermitRequestValidator();
        }

        [Test]
        public void WhenProductsAndUserPermitAreEmpty_ThenValidationErrorIsReturned()
        {
            var permitRequest = new PermitRequest
            {
                Products = [],
                UserPermits = []
            };

            var validationResult = permitRequestValidator.TestValidate(permitRequest);

            validationResult.ShouldHaveValidationErrorFor(x=>x.Products).WithErrorMessage("Products cannot be empty.");
            validationResult.ShouldHaveValidationErrorFor(x=>x.UserPermits).WithErrorMessage("UserPermits cannot be empty.");
        }

        [Test]
        public void WhenValidProductsAndUserPermitsPassedInPermitRequest_ThenNoValidationErrorIsReturned()
        {
            var permitRequest = GetPermitRequests();
            A.CallTo(() => productValidator.Validate(A<Product>._)).Returns(new ValidationResult());
            A.CallTo(() => userPermitValidator.Validate(A<UserPermit>._)).Returns(new ValidationResult());

            var validationResult = permitRequestValidator.TestValidate(permitRequest);

            validationResult.IsValid.Should().BeTrue();
            validationResult.Errors.Should().BeEmpty();
        }

        private static PermitRequest GetPermitRequests()
        {
            return new PermitRequest()
            {
                Products =
                [
                    new Product()
                    {
                        ProductName = "CellCode",
                        EditionNumber = 1,
                        PermitExpiryDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-dd")
                    },
                    new Product()
                    {
                        ProductName = "CellCode1",
                        EditionNumber = 2,
                        PermitExpiryDate = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM-dd")
                    }
                ],
                UserPermits =
                [
                    new UserPermit()
                    {
                        Title = "FakeTitle1",
                        Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                    },
                    new UserPermit()
                    {
                        Title = "FakeTitle2",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                    }
                ]
            };
        }
    }
}
