using System.Security.Cryptography;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class AesEncryption : IAesEncryption
    {
        private const int KeySize = 128, BlockSize = 128, IvLength = 16, HexSize = 32;

        public async Task<string> DecryptAsync(string hexString, string hexKey)
        {
            if(hexString.Length != HexSize)
            {
                throw new AesEncryptionException(EventIds.HexStringLengthError.ToEventId(), "Expected hex string length {HexSize}, but found {HexString Length}.", HexSize, hexString.Length);
            }

            if(hexKey.Length != HexSize)
            {
                throw new AesEncryptionException(EventIds.HexKeyLengthError.ToEventId(), "Expected hex key length {HexSize}, but found {HexKey Length}.", HexSize, hexKey.Length);
            }

            using var aes = CreateAes(hexKey);
            using var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);
            return await PerformCryptographyAsync(hexString, decrypt);
        }

        public async Task<string> EncryptAsync(string hexString, string keyHexEncoded)
        {
            if(hexString.Length != HexSize)
            {
                throw new AesEncryptionException(EventIds.HexStringLengthError.ToEventId(), "Expected hex string length {HexSize}, but found {HexString Length}.", HexSize, hexString.Length);
            }

            if(keyHexEncoded.Length != HexSize)
            {
                throw new AesEncryptionException(EventIds.HexKeyLengthError.ToEventId(), "Expected hex key length {HexSize}, but found {HexKey Length}.", HexSize, keyHexEncoded.Length);
            }

            using var aes = CreateAes(keyHexEncoded);
            using var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            return await PerformCryptographyAsync(hexString, encrypt);
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

        private static async Task<string> PerformCryptographyAsync(string hexString, ICryptoTransform cryptoTransform)
        {
            var cypherBytes = StringToByteArray(hexString);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
            await cryptoStream?.WriteAsync(cypherBytes, 0, cypherBytes.Length);
            await cryptoStream.FlushFinalBlockAsync();
            return BitConverter.ToString(memoryStream.ToArray()).Replace("-", "");
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