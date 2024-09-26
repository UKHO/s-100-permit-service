using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Clients
{
    public interface IProductKeyServiceApiClient
    {
        Task<HttpResponseMessage> GetProductKeysAsync(string uri, List<ProductKeyServiceRequest> productKeyServiceRequest, string accessToken, CancellationToken cancellationToken, string correlationId);
    }
}