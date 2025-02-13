using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Clients
{
    public interface IProductKeyServiceApiClient
    {
        Task<HttpResponseMessage> GetProductKeysAsync(string uri, IEnumerable<ProductKeyServiceRequest> productKeyServiceRequest, string accessToken, string correlationId, CancellationToken cancellationToken);
    }
}