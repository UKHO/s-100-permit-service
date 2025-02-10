using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        Task<IEnumerable<ProductKey>> GetDecryptedKeysFromProductKeysAsync(IEnumerable<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId);
        Task<IEnumerable<UpnInfo>> GetDecryptedHardwareIdFromUserPermitAsync(IEnumerable<UserPermit> userPermits);
        Task<string> CreateEncryptedKeyAsync(string productKeyServiceKey, string hardwareId);
    }
}