using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IProductKeyService
    {
        Task<List<ProductKeyServiceResponse>> GetPermitKeysAsync(List<ProductKeyServiceRequest> productKeyServiceRequest, CancellationToken cancellationToken, string correlationId);
    }
}