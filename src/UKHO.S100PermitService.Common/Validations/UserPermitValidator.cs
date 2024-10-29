using FluentValidation;
using FluentValidation.Results;
using System.Text.RegularExpressions;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validations
{
    public partial class UserPermitValidator : AbstractValidator<UserPermitServiceResponse>, IUserPermitValidator
    {
        private const string Patterns = @"[\\/:*?""<>|]";

        /// <summary>
        /// Validate User Permit details.
        /// </summary>
        /// <remarks>
        /// The user permit is 46 characters long and must be written as ASCII text.
        /// The user permit is created by taking the assigned HW_ID 28 bits (32 characters) and encrypting it with the manufacturer key(M_KEY). The CRC32 algorithm is run on
        /// the encrypted HW_ID and the result appended to it as Check SUM (CRC) (8 characters) .Finally the manufacturer attaches their assigned manufacturer identifier(M_ID) (6 characters) to the end of the resultant string.
        /// If length and checksum validation of user permit failed, Then return ValidationResult as false with error message.
        /// If permit title consists invalid characters or patterns, Then return ValidationResult as false with error message.
        /// </remarks>
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