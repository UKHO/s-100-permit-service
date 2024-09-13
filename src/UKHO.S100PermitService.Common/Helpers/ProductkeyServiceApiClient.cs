using Newtonsoft.Json;
using System.Text;
using UKHO.S100PermitService.Common.Models.ProductkeyService;

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

        public async Task<HttpResponseMessage> GetPermitKeyAsync(string uri, List<ProductKeyServiceRequest> productKeyServiceRequest, string accessToken, string correlationId)
        {
            var payloadJson = JsonConvert.SerializeObject(productKeyServiceRequest);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            httpRequestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            if(!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.SetBearerToken(accessToken);
                httpRequestMessage.AddHeader("X-Correlation-ID", correlationId);
            }

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}