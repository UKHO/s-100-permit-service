using System.Text;

namespace UKHO.S100PermitService.Common.Clients
{
    public class HoldingsApiClient : IHoldingsApiClient
    {
        private readonly HttpClient _httpClient;

        public HoldingsApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<HttpResponseMessage> GetHoldingsAsync(string uri, int licenceId, string accessToken, string correlationId)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequestMessage.Content = new StringContent(licenceId.ToString(), Encoding.UTF8, "application/json");

            if(!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.SetBearerToken(accessToken);
                httpRequestMessage.AddHeader(PermitServiceConstants.XCorrelationIdHeaderKey, correlationId);
            }

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
