namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        string GetHwIdFromUserPermit(string upn);
    }
}