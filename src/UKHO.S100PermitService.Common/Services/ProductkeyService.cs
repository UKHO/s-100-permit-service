using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models.ProductkeyService;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class ProductkeyService: IProductkeyService
    {
        private readonly ILogger<ProductkeyService> _logger;
        private readonly IOptions<ProductkeyServiceApiConfiguration> _productkeyServiceApiConfiguration;
        private readonly IAuthProductKeyServiceTokenProvider _authPksTokenProvider;
        private readonly IProductkeyServiceApiClient _productkeyServiceApiClient;
        private const string KeysEnc = "/keys/s100";

        public ProductkeyService(ILogger<ProductkeyService> logger, IOptions<ProductkeyServiceApiConfiguration> productkeyServiceApiConfiguration, IAuthProductKeyServiceTokenProvider authPksTokenProvider, IProductkeyServiceApiClient productkeyServiceApiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _productkeyServiceApiConfiguration = productkeyServiceApiConfiguration ?? throw new ArgumentNullException(nameof(productkeyServiceApiConfiguration));
            _authPksTokenProvider = authPksTokenProvider ?? throw new ArgumentNullException(nameof(authPksTokenProvider));
            _productkeyServiceApiClient = productkeyServiceApiClient ?? throw new ArgumentNullException(nameof(productkeyServiceApiClient));
        }

        /// <summary>
        /// Get permit key from Product Key Service
        /// </summary>
        /// <param name="productKeyServiceRequest"></param>
        /// <param name="correlationId"></param>
        /// <returns>ProductKeyServiceResponse</returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<ProductKeyServiceResponse>> GetPermitKeyAsync(List<ProductKeyServiceRequest> productKeyServiceRequest, string correlationId)
        {
            _logger.LogInformation(EventIds.GetPermitKeyStarted.ToEventId(), "Request to get permit key from Product Key Service started");

            string bodyJson;
            string uri = _productkeyServiceApiConfiguration.Value.BaseUrl + KeysEnc;
            string accessToken = await _authPksTokenProvider.GetManagedIdentityAuthAsync(_productkeyServiceApiConfiguration.Value.ClientId);

            var httpResponseMessage = await _productkeyServiceApiClient.GetPermitKeyAsync(uri, productKeyServiceRequest, accessToken, correlationId);

            switch(httpResponseMessage.IsSuccessStatusCode)
            {
                case true:
                    {
                        bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        _logger.LogInformation(EventIds.GetPermitKeyCompleted.ToEventId(), "Request to get permit key from Product Key Service completed | StatusCode : {StatusCode}", httpResponseMessage.StatusCode.ToString());

                        var productKeyServiceResponse = JsonConvert.DeserializeObject<List<ProductKeyServiceResponse>>(bodyJson)!;
                        return productKeyServiceResponse;
                    }
                default:
                    {
                        if(httpResponseMessage.StatusCode == HttpStatusCode.BadRequest || httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                        {
                            bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                            _logger.LogError(EventIds.GetPermitKeyException.ToEventId(), "Failed to retrieve permit key for Product Key Service | StatusCode : {StatusCode}| Errors : {ErrorDetails}", httpResponseMessage.StatusCode.ToString(), bodyJson);
                            throw new Exception();
                        }

                        _logger.LogError(EventIds.GetPermitKeyException.ToEventId(), "Failed to retrieve permit key for Product Key Service | StatusCode : {StatusCode}", httpResponseMessage.StatusCode.ToString());
                        throw new Exception();
                    }
            }
        }
    }
}