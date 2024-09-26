namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        string GetEncKeysFromPermitKeys(string permitKeys, string hardwareId);
        string GetHwIdFromUserPermit(string upn, string mKey);
    }
}