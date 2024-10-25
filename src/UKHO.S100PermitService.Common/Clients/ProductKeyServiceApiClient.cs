using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Clients
{
    public class ProductKeyServiceApiClient : IProductKeyServiceApiClient
    {
        private readonly ILogger<ProductKeyServiceApiClient> _logger;
        private readonly HttpClient _httpClient;

        public ProductKeyServiceApiClient(ILogger<ProductKeyServiceApiClient> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Get product keys from Product Key Service.
        /// </summary>
        /// <param name="uri">Request URI.</param>
        /// <param name="productKeyServiceRequest">Product Key Service request body.</param>
        /// <param name="accessToken">Authorization token.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <returns>Product Key Service response.</returns>
        public async Task<HttpResponseMessage> GetProductKeysAsync(string uri, List<ProductKeyServiceRequest> productKeyServiceRequest, string accessToken, CancellationToken cancellationToken, string correlationId)
        {
            var payloadJson = JsonSerializer.Serialize(productKeyServiceRequest);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            httpRequestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, PermitServiceConstants.ContentType);

            if(!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.SetBearerToken(accessToken);
                httpRequestMessage.AddHeader(PermitServiceConstants.XCorrelationIdHeaderKey, correlationId);
            }
            else
            {
                _logger.LogWarning(EventIds.MissingAccessToken.ToEventId(), "Access token is empty or null.");
            }

            return await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}