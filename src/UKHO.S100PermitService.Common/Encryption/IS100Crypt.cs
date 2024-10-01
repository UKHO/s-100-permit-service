namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        string GetDecryptedHardwareIdFromUserPermit(string upn);
    }
}