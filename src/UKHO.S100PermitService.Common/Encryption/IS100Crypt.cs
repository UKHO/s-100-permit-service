using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.Request;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        Task<IEnumerable<ProductKey>> GetDecryptedKeysFromProductKeysAsync(IEnumerable<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId);
        Task<IEnumerable<UpnInfo>> GetDecryptedHardwareIdFromUserPermitAsync(UserPermitServiceResponse userPermitServiceResponse);
        Task<string> CreateEncryptedKeyAsync(string productKeyServiceKey, string hardwareId);
    }
}