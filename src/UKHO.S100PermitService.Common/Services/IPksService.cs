using UKHO.S100PermitService.Common.Models.Pks;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPksService
    {
        Task<List<ProductKeyServiceResponse>> GetPermitKeyAsync(List<ProductKeyServiceRequest> productKeyServiceRequest);
    }
}