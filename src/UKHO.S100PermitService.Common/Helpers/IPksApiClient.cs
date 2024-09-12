using UKHO.S100PermitService.Common.Models.Pks;

namespace UKHO.S100PermitService.Common.Helpers
{
    public interface IPksApiClient
    {
        Task<HttpResponseMessage> GetPermitKeyAsync(string uri, List<ProductKeyServiceRequest> productKeyServiceRequest, string accessToken, string correlationId = null);
    }
}