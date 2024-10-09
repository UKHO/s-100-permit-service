using FluentValidation;
using FluentValidation.Results;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validations
{
    public class UserPermitValidator : AbstractValidator<UserPermitServiceResponse>, IUserPermitValidator
    {
        public UserPermitValidator()
        {
            RuleForEach(x => x.UserPermits).ChildRules(userPermits =>
            {
                userPermits.RuleFor(userPermit => userPermit.Upn).NotNull().Length(46)
                    .WithMessage(userPermit => $"Invalid UPN found for: {userPermit.Title}. UPN must be 46 characters long")
                    .DependentRules(() =>
                    {
                        userPermits.RuleFor(userPermit => userPermit.Upn)
                            .Must(ChecksumValidation.IsValid).WithMessage(userPermit => $"Invalid checksum found for: {userPermit.Title}");
                    });
            });
        }

        ValidationResult IUserPermitValidator.Validate(UserPermitServiceResponse userPermitServiceResponse)
        {
            return Validate(userPermitServiceResponse);
        }
    }
}