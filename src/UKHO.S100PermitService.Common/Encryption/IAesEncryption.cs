namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IAesEncryption
    {
        string Decrypt(string hexString, string keyHexEncoded);

        string Encrypt(string hexString, string hexKey);
    }
}