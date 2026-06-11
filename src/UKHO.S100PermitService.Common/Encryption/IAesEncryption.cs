namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IAesEncryption
    {
        Task<string> DecryptAsync(string hexString, string keyHexEncoded, string productName);

        Task<string> EncryptAsync(string hexString, string keyHexEncoded);
    }
}
