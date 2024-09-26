namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        string GetEncKeysFromPermitKeys(string hexString, string keyHexEncoded);

        string GetHwIdFromUserPermit(string encryptedHardwareId, string mKey);
    }
}