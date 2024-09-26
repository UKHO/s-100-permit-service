using FluentValidation;
using ICSharpCode.SharpZipLib.Checksum;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validation
{
    public class UserPermitValidator : AbstractValidator<UserPermitFields>, IUserPermitValidator
    {
        public UserPermitValidator()
        {
            RuleFor(userPermit => userPermit.Upn).NotNull().Length(46)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Invalid UPN. UPN must be 46 characters long.");

            RuleFor(userPermit => userPermit.EncryptedHardwareId).NotNull().Length(32)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Invalid UPN. Encrypted HardwareId must be 32 characters long.");

            RuleFor(userPermit => userPermit.CheckSum).NotNull().Length(8)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Invalid UPN. CheckSum must be 8 characters long.");

            RuleFor(userPermit => userPermit.CheckSum).NotNull().Length(8)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Invalid UPN. CheckSum must be 8 characters long.");

            RuleFor(userPermit => userPermit.CheckSum).Must((userPermit, s) => IsValidChecksum(userPermit.EncryptedHardwareId, userPermit.CheckSum)).WithMessage("Invalid checksum");

            RuleFor(userPermit => userPermit.MId).NotNull().Length(6)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Invalid UPN. MId must be 6 characters long.");
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