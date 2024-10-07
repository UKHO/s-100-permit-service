using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace UKHO.S100PermitService.Common.Encryption
{
    [ExcludeFromCodeCoverage]
    public class AesEncryption : IAesEncryption
    {
        private const int KeySize = 128, BlockSize = 128, IvLength = 16;

        public string Decrypt(string hexString, string keyHexEncoded)
        {
            using var aes = CreateAes(keyHexEncoded);
            using var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);
            return PerformCryptography(hexString, decrypt);
        }

        public string Encrypt(string hexString, string keyHexEncoded)
        {
            using var aes = CreateAes(keyHexEncoded);
            using var decrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            return PerformCryptography(hexString, decrypt);
        }

        private static Aes CreateAes(string keyHexEncoded)
        {
            var aes = Aes.Create();
            aes.BlockSize = BlockSize;
            aes.KeySize = KeySize;
            aes.IV = new byte[IvLength];
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;
            aes.Key = StringToByteArray(keyHexEncoded);
            return aes;
        }

        private static string PerformCryptography(string hexString, ICryptoTransform cryptoTransform)
        {
            var cypherBytes = StringToByteArray(hexString);

            using var ms = new MemoryStream();
            using var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(cypherBytes, 0, cypherBytes.Length);
            cryptoStream.FlushFinalBlock();
            return BitConverter.ToString(ms.ToArray()).Replace("-", "");
        }

        private static byte[] StringToByteArray(string hexString)
        {
            return Enumerable.Range(0, hexString.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}