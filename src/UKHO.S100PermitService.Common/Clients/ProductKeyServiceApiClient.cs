using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Clients
{
    public class ProductKeyServiceApiClient(ILogger<ProductKeyServiceApiClient> logger, HttpClient httpClient) : IProductKeyServiceApiClient
    {
        public async Task<HealthStatus> GetHealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync("health", cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var productKeyServiceHealthResponse = JsonSerializer.Deserialize<ProductKeyServiceHealthResponse>(content);

            return productKeyServiceHealthResponse.HealthStatus;
        }

        /// <summary>
        /// Get product keys from Product Key Service.
        /// </summary>
        /// <param name="uri">Request URI.</param>
        /// <param name="productKeyServiceRequest">Product Key Service request body.</param>
        /// <param name="accessToken">Authorization token.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Product Key Service response.</returns>
        public async Task<HttpResponseMessage> GetProductKeysAsync(string uri, IEnumerable<ProductKeyServiceRequest> productKeyServiceRequest, string accessToken, string correlationId, CancellationToken cancellationToken)
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
                logger.LogWarning(EventIds.MissingAccessToken.ToEventId(), "Access token is empty or null.");
            }

            return await httpClient.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}