using FluentValidation;
using FluentValidation.Results;
using System.Globalization;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Validations
{
    public class ProductValidator : AbstractValidator<Product>, IProductValidator
    {
        /// <summary>
        /// Validate Product details.
        /// </summary>
        /// <remarks>
        /// The ProductName field must be non-empty and not exceed 255 characters.
        /// The EditionNumber must be a positive integer.
        /// The PermitExpiryDate must be a valid date in the format YYYY-MM-DD and represent a date i.e. Either today or in the future.
        /// </remarks>
        public ProductValidator()
        {
            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("ProductName cannot be empty.")
                .MaximumLength(255).WithMessage("Must be a maximum of 255 characters.");

            RuleFor(x => x.EditionNumber)
                .GreaterThan(0).WithMessage("Must be a natural number i.e. positive number greater than 0.");

            RuleFor(x => x.PermitExpiryDate)
                .NotEmpty().WithMessage("PermitExpiryDate cannot be empty.")
                .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Must be in UTC format YYYY-MM-DD.")
                .DependentRules(() =>
                    RuleFor(x => x.PermitExpiryDate)
                        .Must(BeAValidDate).WithMessage("Must be a valid date.")
                        .DependentRules(() => RuleFor(x => x.PermitExpiryDate)
                            .Must(BeTodayOrLater).WithMessage("Must be today or a future date.")
                        )
                );
        }

        ValidationResult IProductValidator.Validate(Product product)
        {
            return Validate(product);
        }

        /// <summary>
        /// Checks if the given date string is a valid date in the format YYYY-MM-DD.
        /// </summary>
        /// <param name="date">The date string to validate.</param>
        /// <returns>True if the date is valid, otherwise false.</returns>
        private static bool BeAValidDate(string date)
        {
            return DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// Checks if the given date string is today or a future date.
        /// </summary>
        /// <param name="date">The date string to validate.</param>
        /// <returns>True if the date is today or a future date, otherwise false.</returns>
        private static bool BeTodayOrLater(string date)
        {
            return DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) && parsedDate >= DateTime.UtcNow.Date;
        }
    }
}