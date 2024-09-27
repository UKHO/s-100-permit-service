using FluentValidation;
using ICSharpCode.SharpZipLib.Checksum;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validation
{
    public class UserPermitValidator : AbstractValidator<UserPermitServiceResponse>, IUserPermitValidator
    {
        private const int EncryptedHardwareIdLength = 32;
        private const int ReverseChecksumIndex = 6;
        public UserPermitValidator()
        {
            RuleForEach(x => x.UserPermits).ChildRules(userPermits =>
            {
                userPermits.RuleFor(userPermit => userPermit.Upn).NotNull().Length(46)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Invalid UPN. UPN must be 46 characters long.");
            });

            RuleForEach(x => x.UserPermits).ChildRules(userPermits =>
            {
                userPermits.RuleFor(userPermit => userPermit.Upn)
                .Must((userPermit, s) => IsValidChecksum(userPermit.Upn[..EncryptedHardwareIdLength], userPermit.Upn[EncryptedHardwareIdLength..^ReverseChecksumIndex])).WithMessage("Invalid checksum");
            });
        }

        public static bool IsValidChecksum(string hwIdEncrypted, string checkSum)
        {
            var crc = new Crc32();
            crc.Update(Encoding.UTF8.GetBytes(hwIdEncrypted));
            var calculatedChecksum = crc.Value.ToString("X8");
            return calculatedChecksum.Equals(checkSum);
        }
    }
}