using System.Security.Cryptography;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class AesEncryption : IAesEncryption
    {
        private const int KeySize = 128, BlockSize = 128, IvLength = 16, HexSize = 32;
        private const string NewValue = "";

        /// <summary>
        /// Get decrypted data.
        /// </summary>
        /// <remarks>
        /// Decrypt data from encrypted data using secret key.
        /// Advanced Encryption Standard (AES) block cipher algorithm is used.This is a symmetric-key algorithm. This means that the same key is used for encryption and decryption.
        /// Cipher Block Chaining(CBC) mode of operation and No padding is used.
        /// For S-100 size of data and secret key is fixed to 128 bits (32 characters) hexadecimal digits, if validation fails then AesEncryptionException exception will be thrown.
        /// </remarks>
        /// <param name="hexString">Data to be decrypt.</param>
        /// <param name="hexKey">Secret Key.</param>
        /// <returns>Decrypted data.</returns>
        /// <exception cref="AesEncryptionException">AesEncryptionException exception will be thrown when length validation fails.</exception>
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

        /// <summary>
        /// Get encrypted data.
        /// </summary>
        /// <remarks>
        /// Encrypt data using secret key.
        /// Advanced Encryption Standard (AES) block cipher algorithm is used.This is a symmetric-key algorithm. This means that the same key is used for encryption and decryption.
        /// Cipher Block Chaining(CBC) mode of operation and No padding is used.
        /// For S-100 size of data and secret key is fixed to 128 bits (32 characters) hexadecimal digits, if validation fails then AesEncryptionException exception will be thrown.
        /// </remarks>
        /// <param name="hexString">Data to be encrypt.</param>
        /// <param name="keyHexEncoded">Secret Key.</param>
        /// <returns>Encrypted data.</returns>
        /// <exception cref="AesEncryptionException">AesEncryptionException exception will be thrown when length validation fails.</exception>
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

        /// <summary>
        /// Create Advanced Encryption Standard (AES) object.
        /// </summary>
        /// <remarks>
        /// Advanced Encryption Standard (AES) block cipher algorithm is used.This is a symmetric-key algorithm. This means that the same key is used for encryption and decryption.
        /// Cipher Block Chaining(CBC) mode of operation and No padding is used.
        /// </remarks>
        /// <param name="keyHexEncoded">Secret Key.</param>
        /// <returns>AES object</returns>
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

        /// <summary>
        /// Perform operations of cryptographic transformations.
        /// </summary>
        /// <param name="hexString">Data to transform.</param>
        /// <param name="cryptoTransform">Cryptographic object to perform transformations.</param>
        /// <returns>Hexadecimal string</returns>
        private static async Task<string> PerformCryptographyAsync(string hexString, ICryptoTransform cryptoTransform)
        {
            var cypherBytes = StringToByteArray(hexString);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
            await cryptoStream?.WriteAsync(cypherBytes, 0, cypherBytes.Length);
            await cryptoStream.FlushFinalBlockAsync();
            return BitConverter.ToString(memoryStream.ToArray()).Replace("-", NewValue);
        }

        /// <summary>
        /// Convert hexadecimal string into a byte array.
        /// </summary>
        /// <param name="hexString">Hexadecimal string data.</param>
        /// <returns>Byte array from hexadecimal string.</returns>
        private static byte[] StringToByteArray(string hexString)
        {
            return Enumerable.Range(0, hexString.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}