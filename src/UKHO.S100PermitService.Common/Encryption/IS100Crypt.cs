namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        string DecryptData(string hexString, string keyHexEncoded);
    }
}