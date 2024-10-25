using Microsoft.Extensions.Logging;
using System.Text;
using UKHO.S100PermitService.Common.Events;

namespace UKHO.S100PermitService.Common.Clients
{
    public class UserPermitApiClient : IUserPermitApiClient
    {
        private readonly ILogger<UserPermitApiClient> _logger;
        private readonly HttpClient _httpClient;

        public UserPermitApiClient(ILogger<UserPermitApiClient> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<HttpResponseMessage> GetUserPermitsAsync(string uri, int licenceId, string accessToken, CancellationToken cancellationToken, string correlationId)
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
