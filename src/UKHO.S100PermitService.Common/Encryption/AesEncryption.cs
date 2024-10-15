using System.Security.Cryptography;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class AesEncryption : IAesEncryption
    {
        private const int KeySize = 128, BlockSize = 128, IvLength = 16, HexSize = 32;

        public string Decrypt(string hexString, string hexKey)
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
            return PerformCryptography(hexString, decrypt);
        }

        public string Encrypt(string hexString, string hexKey)
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
            using var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            return PerformCryptography(hexString, encrypt);
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

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(cypherBytes, 0, cypherBytes.Length);
            cryptoStream.FlushFinalBlock();
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