using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace UKHO.S100PermitService.Common.Encryption
{
    [ExcludeFromCodeCoverage]
    public class AesEncryption : IAesEncryption
    {
        private const int KeySize = 128;
        private const int Iv_Length = 16;

        public string Decrypt(string hexString, string keyHexEncoded)
        {
            using var aes = Aes.Create();
            aes.BlockSize = KeySize;
            aes.KeySize = KeySize;
            aes.IV = new byte[Iv_Length];
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = StringToByteArray(keyHexEncoded);

            // decryption
            var encryptedByte = StringToByteArray(hexString);

            using var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);
            var decryptedText = decrypt.TransformFinalBlock(encryptedByte, 0, encryptedByte.Length);

            return BitConverter.ToString((decryptedText)).Replace("-", "");
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