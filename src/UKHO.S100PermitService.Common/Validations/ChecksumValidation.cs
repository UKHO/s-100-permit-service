using ICSharpCode.SharpZipLib.Checksum;
using System.Text;

namespace UKHO.S100PermitService.Common.Validations
{
    public static class ChecksumValidation
    {
        private const int EncryptedHardwareIdLength = 32;
        private const int ReverseChecksumIndex = 6;

        public static bool IsValid(string upn)
        {
            var hwIdEncrypted = upn[..EncryptedHardwareIdLength];
            var checksum = upn[EncryptedHardwareIdLength..^ReverseChecksumIndex];

            var crc = new Crc32();
            crc.Update(Encoding.UTF8.GetBytes(hwIdEncrypted));
            var calculatedChecksum = crc.Value.ToString("X8");
            return calculatedChecksum.Equals(checksum);
        }
    }
}