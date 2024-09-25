namespace UKHO.S100PermitService.Common.Securities
{
    public interface IS100Manufacturer
    {
        string DecryptData(string hexString, string keyHexEncoded);
    }
}