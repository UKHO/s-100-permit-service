namespace UKHO.S100PermitService.Common.Securities
{
    public interface IS100Crypt
    {
        string Decrypt(string hexString, string keyHexEncoded);
    }
}