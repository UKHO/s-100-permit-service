using System.Text;

namespace UKHO.S100PermitService.Common.Clients
{
    public class ProductKeyServiceApiClient : IProductKeyServiceApiClient
    {
        private readonly HttpClient _httpClient;

        public ProductKeyServiceApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<HttpResponseMessage> CallProductKeyServiceApiAsync(string uri, HttpMethod httpMethod, string payload, string accessToken, string correlationId)
        {
            using var httpRequestMessage = new HttpRequestMessage(httpMethod, uri);
            httpRequestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            if(!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.SetBearerToken(accessToken);
                httpRequestMessage.AddHeader("X-Correlation-ID", correlationId);
            }

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}