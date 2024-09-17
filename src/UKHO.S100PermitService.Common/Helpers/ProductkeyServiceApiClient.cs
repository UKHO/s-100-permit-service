using System.Text;

namespace UKHO.S100PermitService.Common.Helpers
{
    public class ProductkeyServiceApiClient : IProductkeyServiceApiClient
    {
        private readonly HttpClient _httpClient;

        public ProductkeyServiceApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
        }

        public async Task<HttpResponseMessage> CallProductkeyServiceApiAsync(string uri, HttpMethod httpMethod, string payload, string accessToken, string correlationId)
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