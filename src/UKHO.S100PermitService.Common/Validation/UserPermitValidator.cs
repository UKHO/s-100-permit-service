using FluentValidation;
using FluentValidation.Results;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validation
{
    public class UserPermitValidator : AbstractValidator<UserPermitServiceResponse>, IUserPermitValidator
    {
        public UserPermitValidator()
        {
            RuleForEach(x => x.UserPermits).ChildRules(userPermits =>
            {
                userPermits.RuleFor(userPermit => userPermit.Upn).NotNull().Length(46)
                    .WithMessage("Invalid UPN. UPN must be 46 characters long")
                    .DependentRules(() =>
                    {
                        userPermits.RuleFor(userPermit => userPermit.Upn)
                            .Must(ChecksumValidation.IsValidChecksum).WithMessage("Invalid checksum");
                    });
            });
        }

        ValidationResult IUserPermitValidator.Validate(UserPermitServiceResponse userPermitServiceResponse)
        {
            return Validate(userPermitServiceResponse);
        }
    }
}