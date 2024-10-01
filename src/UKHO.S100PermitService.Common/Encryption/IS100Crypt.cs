using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        List<UpnInfo> GetDecryptedHardwareIdFromUserPermit(UserPermitServiceResponse userPermitServiceResponse);
    }
}