using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.ProductkeyService;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class ProductkeyService : IProductkeyService
    {
        private readonly ILogger<ProductkeyService> _logger;
        private readonly IOptions<ProductkeyServiceApiConfiguration> _productkeyServiceApiConfiguration;
        private readonly IProductKeyServiceAuthTokenProvider _productKeyServiceAuthTokenProvider;
        private readonly IProductkeyServiceApiClient _productkeyServiceApiClient;
        private const string KeysEnc = "/keys/s100";

        public ProductkeyService(ILogger<ProductkeyService> logger, IOptions<ProductkeyServiceApiConfiguration> productkeyServiceApiConfiguration, IProductKeyServiceAuthTokenProvider productKeyServiceAuthTokenProvider, IProductkeyServiceApiClient productkeyServiceApiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _productkeyServiceApiConfiguration = productkeyServiceApiConfiguration ?? throw new ArgumentNullException(nameof(productkeyServiceApiConfiguration));
            _productKeyServiceAuthTokenProvider = productKeyServiceAuthTokenProvider ?? throw new ArgumentNullException(nameof(productKeyServiceAuthTokenProvider));
            _productkeyServiceApiClient = productkeyServiceApiClient ?? throw new ArgumentNullException(nameof(productkeyServiceApiClient));
        }

        /// <summary>
        /// Get permit key from Product Key Service
        /// </summary>
        /// <param name="productKeyServiceRequest"></param>
        /// <param name="correlationId"></param>
        /// <returns>ProductKeyServiceResponse</returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<ProductKeyServiceResponse>> PostProductKeyServiceRequest(List<ProductKeyServiceRequest> productKeyServiceRequest, string correlationId)
        {
            var uri = new Uri(_productkeyServiceApiConfiguration.Value.BaseUrl + KeysEnc);

            _logger.LogInformation(EventIds.ProductKeyServicePostPermitKeyRequestStarted.ToEventId(), "Request to ProductKeyService POST Uri : {RequestUri} started.", uri.AbsoluteUri);

            var accessToken = await _productKeyServiceAuthTokenProvider.GetManagedIdentityAuthAsync(_productkeyServiceApiConfiguration.Value.ClientId);

            var payloadJson = JsonConvert.SerializeObject(productKeyServiceRequest);

            var httpResponseMessage = await _productkeyServiceApiClient.CallProductkeyServiceApiAsync(uri.AbsoluteUri, HttpMethod.Post, payloadJson, accessToken, correlationId);

            if(httpResponseMessage.IsSuccessStatusCode)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                _logger.LogInformation(EventIds.ProductKeyServicePostPermitKeyRequestCompleted.ToEventId(), "Request to ProductKeyService POST Uri : {RequestUri} completed. | StatusCode : {StatusCode}", uri.AbsoluteUri, httpResponseMessage.StatusCode.ToString());

                var productKeyServiceResponse = JsonConvert.DeserializeObject<List<ProductKeyServiceResponse>>(bodyJson)!;
                return productKeyServiceResponse;
            }

            if(httpResponseMessage.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                throw new PermitServiceException(EventIds.ProductKeyServicePostPermitKeyRequestFailed.ToEventId(),
                    "Request to ProductKeyService POST Uri : {0} failed. | StatusCode : {1} | Error Details : {2}",
                    uri.AbsoluteUri, httpResponseMessage.StatusCode.ToString(), bodyJson);
            }

            throw new PermitServiceException(EventIds.ProductKeyServicePostPermitKeyRequestFailed.ToEventId(), 
                "Request to ProductKeyService POST Uri : {0} failed. | StatusCode : {1}", 
                uri.AbsoluteUri, httpResponseMessage.StatusCode.ToString());
        }
    }
}