using FluentValidation;
using FluentValidation.Results;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Validations
{
    public class PermitRequestValidator : AbstractValidator<PermitRequest>, IPermitRequestValidator
    {
        /// <summary>
        ///Validate Permit Request details.
        /// </summary>
        /// <remarks>
        /// The Products list must not be empty, and each individual product within the list is validated using the ProductValidator/>.
        /// The UserPermits list must not be empty, and each individual user permit is validated using the UserPermitValidator/>.
        /// </remarks>
        public PermitRequestValidator()
        {
            RuleFor(x => x.Products)
                .NotEmpty().WithMessage("Products cannot be empty.");

            RuleForEach(x => x.Products).SetValidator(new ProductValidator());

            RuleFor(x => x.UserPermits)
                .NotEmpty().WithMessage("UserPermits cannot be empty.");

            RuleForEach(x => x.UserPermits).SetValidator(new UserPermitValidator());
        }

        public async Task<ValidationResult> ValidateAsync(PermitRequest permitRequest)
        {
            return await ValidateAsync(permitRequest);
        }
    }
}