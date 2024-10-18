using FluentValidation;
using FluentValidation.Results;
using System.Text.RegularExpressions;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validations
{
    public partial class UserPermitValidator : AbstractValidator<UserPermitServiceResponse>, IUserPermitValidator
    {
        private const string Patterns = @"[\\/:*?""<>|]";

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

                        userPermits.RuleFor(userPermit => userPermit.Title)
                            .Must(title => !IsTitleContainsInValidCharacters().IsMatch(title)).WithMessage(userPermit => $"Invalid title found : {userPermit.Title}");
                    });
            });
        }

        ValidationResult IUserPermitValidator.Validate(UserPermitServiceResponse userPermitServiceResponse)
        {
            return Validate(userPermitServiceResponse);
        }

        [GeneratedRegex(Patterns)]
        private static partial Regex IsTitleContainsInValidCharacters();
    }
}