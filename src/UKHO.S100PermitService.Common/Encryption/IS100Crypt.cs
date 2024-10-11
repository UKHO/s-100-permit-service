using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        IEnumerable<ProductKey> GetDecryptedKeysFromProductKeys(IEnumerable<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId);
        IEnumerable<UpnInfo> GetDecryptedHardwareIdFromUserPermit(UserPermitServiceResponse userPermitServiceResponse);
    }
}