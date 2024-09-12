using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models.Pks;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class PksService : IPksService
    {
        private readonly ILogger<PksService> _logger;
        private readonly IOptions<PksApiConfiguration> _pksApiConfiguration;
        private readonly IAuthProductKeyServiceTokenProvider _authPksTokenProvider;
        private readonly IPksApiClient _pksApiClient;
        private const string KeysEnc = "/keys/s100";

        public PksService(ILogger<PksService> logger, IOptions<PksApiConfiguration> pksApiConfiguration, IAuthProductKeyServiceTokenProvider authPksTokenProvider, IPksApiClient pksApiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pksApiConfiguration = pksApiConfiguration ?? throw new ArgumentNullException(nameof(pksApiConfiguration));
            _authPksTokenProvider = authPksTokenProvider ?? throw new ArgumentNullException(nameof(authPksTokenProvider));
            _pksApiClient = pksApiClient ?? throw new ArgumentNullException(nameof(pksApiClient));
        }

        public async Task<List<ProductKeyServiceResponse>> GetPermitKeyAsync(List<ProductKeyServiceRequest> productKeyServiceRequest)
        {
            _logger.LogInformation(EventIds.GetPermitKeyStarted.ToEventId(), "Request to get permit key from Product Key Service started");

            string bodyJson;
            string uri = _pksApiConfiguration.Value.BaseUrl + KeysEnc;
            string accessToken = "";////await _authPksTokenProvider.GetManagedIdentityAuthAsync(_pksApiConfiguration.Value.ClientId);

            var httpResponseMessage = await _pksApiClient.GetPermitKeyAsync(uri, productKeyServiceRequest, accessToken);

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
                        if(httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
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
