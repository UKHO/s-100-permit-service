using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class HoldingsService : IHoldingsService
    {
        private readonly ILogger<HoldingsService> _logger;
        private readonly IOptions<HoldingsServiceApiConfiguration> _holdingsServiceApiConfiguration;
        private readonly IAuthHoldingsServiceTokenProvider _authHoldingsServiceTokenProvider;
        private readonly IHoldingsApiClient _holdingsApiClient;
        private const string HoldingUrl = "/holdings/{0}/s100";

        public HoldingsService(ILogger<HoldingsService> logger, IOptions<HoldingsServiceApiConfiguration> holdingsApiConfiguration, IAuthHoldingsServiceTokenProvider authHoldingsServiceTokenProvider, IHoldingsApiClient holdingsApiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsServiceApiConfiguration = holdingsApiConfiguration ?? throw new ArgumentNullException(nameof(holdingsApiConfiguration));
            _authHoldingsServiceTokenProvider = authHoldingsServiceTokenProvider ?? throw new ArgumentNullException(nameof(authHoldingsServiceTokenProvider));
            _holdingsApiClient = holdingsApiClient ?? throw new ArgumentNullException(nameof(holdingsApiClient));
        }

        public async Task<List<HoldingsServiceResponse>> GetHoldingsAsync(int licenceId, string correlationId)
        {
            _logger.LogInformation(EventIds.GetHoldingsToHoldingsServiceStarted.ToEventId(),
                "Request to get holdings to Holdings Service started | _X-Correlation-ID : {CorrelationId}", correlationId);

            string bodyJson;
            var uri = _holdingsServiceApiConfiguration.Value.BaseUrl + string.Format(HoldingUrl, licenceId);
            var accessToken = await _authHoldingsServiceTokenProvider.GetManagedIdentityAuthAsync(_holdingsServiceApiConfiguration.Value.HoldingsClientId);

            var httpResponseMessage = await _holdingsApiClient.GetHoldingsAsync(uri, licenceId, accessToken, correlationId);

            switch(httpResponseMessage.IsSuccessStatusCode)
            {
                case true:
                    {
                        bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        _logger.LogInformation(EventIds.GetHoldingsToHoldingsServiceCompleted.ToEventId(), "Request to get holdings to Holdings Service completed | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), correlationId);

                        var holdingsServiceResponse = JsonConvert.DeserializeObject<List<HoldingsServiceResponse>>(bodyJson);

                        return holdingsServiceResponse;
                    }
                default:
                    {
                        if(httpResponseMessage.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                        {
                            bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                            _logger.LogError(EventIds.GetHoldingsToHoldingsServiceFailed.ToEventId(), "Failed to get holdings from Holdings Service | StatusCode : {StatusCode} | Errors : {ErrorDetails} | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), bodyJson, correlationId);
                            throw new Exception("Failed to get holdings from Holdings Service | StatusCode : {StatusCode}| Errors : {ErrorDetails}");
                        }

                        _logger.LogError(EventIds.GetHoldingsToHoldingsServiceFailed.ToEventId(), "Failed to get holdings from Holdings Service | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), correlationId);
                        throw new Exception("Failed to get holding data");
                    }
            }
        }
    }
}
