using UKHO.S100PermitService.Common.Models.ProductkeyService;

namespace UKHO.S100PermitService.Common.Helpers
{
    public interface IProductkeyServiceApiClient
    {
        Task<HttpResponseMessage> GetPermitKeyAsync(string uri, List<ProductKeyServiceRequest> productKeyServiceRequest, string accessToken, string correlationId);
    }
}