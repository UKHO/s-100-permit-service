using UKHO.S100PermitService.Common.Models.ProductkeyService;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IProductkeyService
    {
        Task<List<ProductKeyServiceResponse>> GetPermitKeyAsync(List<ProductKeyServiceRequest> productKeyServiceRequest, string correlationId);
    }
}