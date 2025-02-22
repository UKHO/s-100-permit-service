using FluentValidation;
using FluentValidation.Results;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Validations
{
    public class PermitRequestValidator : AbstractValidator<PermitRequest>, IPermitRequestValidator
    {
        public PermitRequestValidator()
        {
            RuleFor(x => x.Products)
                .NotEmpty().WithMessage("Products cannot be empty.");

            RuleForEach(x => x.Products).SetValidator(new ProductValidator());

            RuleFor(x => x.UserPermits)
                .NotEmpty().WithMessage("UserPermits cannot be empty.");

            RuleForEach(x => x.UserPermits).SetValidator(new UserPermitValidator());
        }

        ValidationResult IPermitRequestValidator.Validate(PermitRequest permitRequest)
        {
            return Validate(permitRequest);
        }
    }
}