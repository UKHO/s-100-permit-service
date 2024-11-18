using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IProductKeyService
    {
        Task<ServiceResponseResult<List<ProductKeyServiceResponse>>> GetProductKeysAsync(List<ProductKeyServiceRequest> productKeyServiceRequest, string correlationId, CancellationToken cancellationToken);
    }
}