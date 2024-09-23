using UKHO.S100PermitService.Common.Models.ProductKeyServices;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IProductKeyService
    {
        Task<List<ProductKeyServiceResponse>> PostProductKeyServiceRequest(List<ProductKeyServiceRequests> productKeyServiceRequest, string correlationId);
    }
}
