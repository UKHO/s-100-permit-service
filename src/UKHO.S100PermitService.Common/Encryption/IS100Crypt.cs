using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Encryption
{
    public interface IS100Crypt
    {
        List<ProductKeyServiceResponse> GetEncKeysFromPermitKeys(List<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId);
    }
}