using Microsoft.Extensions.Logging;
using System.Text;
using UKHO.S100PermitService.Common.Events;

namespace UKHO.S100PermitService.Common.Clients
{
    public class HoldingsApiClient : IHoldingsApiClient
    {
        private readonly ILogger<HoldingsApiClient> _logger;
        private readonly HttpClient _httpClient;

        public HoldingsApiClient(ILogger<HoldingsApiClient> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Get Holding details from Shop Facade Holding Service for requested licence id.
        /// </summary>
        /// <param name="uri">Request URI.</param>
        /// <param name="licenceId">Requested licence id.</param>
        /// <param name="accessToken">Authorization token.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <returns>Shop Facade Holding Service response.</returns>
        public async Task<HttpResponseMessage> GetHoldingsAsync(string uri, int licenceId, string accessToken, CancellationToken cancellationToken, string correlationId)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequestMessage.Content = new StringContent(licenceId.ToString(), Encoding.UTF8, PermitServiceConstants.ContentType);

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