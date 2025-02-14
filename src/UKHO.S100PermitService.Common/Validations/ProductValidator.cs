using FluentValidation;
using FluentValidation.Results;
using System.Globalization;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Validations
{
    public class ProductValidator : AbstractValidator<Product>, IProductValidator
    {
        public ProductValidator()
        {
            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("ProductName cannot be empty.")
                .MaximumLength(255).WithMessage("Must be a maximum of 255 characters.");

            RuleFor(x => x.EditionNumber)
                .GreaterThan(0).WithMessage("Must be a natural number i.e. positive number greater than 0.");

            RuleFor(x => x.PermitExpiryDate)
                .NotEmpty().WithMessage("Must be in UTC format YYYY-MM-DD.");

            RuleFor(x => x.PermitExpiryDate)
                .NotEmpty().WithMessage("PermitExpiryDate cannot be empty.")
                .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Must be in UTC format YYYY-MM-DD.")
                .Must(BeAValidDate).WithMessage("Must be a valid date.");
        }

        ValidationResult IProductValidator.Validate(Product product)
        {
            return Validate(product);
        }

        private bool BeAValidDate(string date)
        {
            return DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }
    }
}