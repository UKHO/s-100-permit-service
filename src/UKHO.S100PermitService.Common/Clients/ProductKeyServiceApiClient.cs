using Newtonsoft.Json;
using System.Text;
using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Clients
{
    public class ProductKeyServiceApiClient : IProductKeyServiceApiClient
    {
        private readonly HttpClient _httpClient;

        public ProductKeyServiceApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<HttpResponseMessage> GetProductKeysAsync(string uri, List<ProductKeyServiceRequest> productKeyServiceRequest, string accessToken, CancellationToken cancellationToken, string correlationId)
        {
            var payloadJson = JsonConvert.SerializeObject(productKeyServiceRequest);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            httpRequestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, PermitServiceConstants.ContentType);

            if(!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.SetBearerToken(accessToken);
                httpRequestMessage.AddHeader(PermitServiceConstants.XCorrelationIdHeaderKey, correlationId);
            }

            return await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}