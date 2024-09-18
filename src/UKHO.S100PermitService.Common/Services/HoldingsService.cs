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

        public async Task<List<HoldingsServiceResponse>> GetHoldings(int licenceId, string correlationId)
        {
            _logger.LogInformation(EventIds.GetHoldingsDataToHoldingsService.ToEventId(),
                "Get holdings data to Holdings Service started.");

            string bodyJson;
            var uri = _holdingsServiceApiConfiguration.Value.BaseUrl + string.Format(HoldingUrl, licenceId);
            var accessToken = await _authHoldingsServiceTokenProvider.GetManagedIdentityAuthAsync(_holdingsServiceApiConfiguration.Value.HoldingsClientId);

            var httpResponseMessage = await _holdingsApiClient.GetHoldingsDataAsync(uri, licenceId, accessToken, correlationId);

            switch(httpResponseMessage.IsSuccessStatusCode)
            {
                case true:
                    {
                        bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        _logger.LogInformation(EventIds.GetHoldingsDataToHoldingsCompleted.ToEventId(), "Request to get holdings data to Holdings Service completed | StatusCode : {StatusCode}", httpResponseMessage.StatusCode.ToString());

                        var holdingsServiceResponse = JsonConvert.DeserializeObject<List<HoldingsServiceResponse>>(bodyJson);

                        return holdingsServiceResponse;
                    }
                default:
                    {
                        if(httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
                        {
                            bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                            _logger.LogError(EventIds.GetHoldingsDataToHoldingsFailed.ToEventId(), "Failed to retrieve get holdings data with | StatusCode : {StatusCode}| Errors : {ErrorDetails} for Holdings Service.", httpResponseMessage.StatusCode.ToString(), bodyJson);
                            throw new Exception("Failed to retrieve get holdings data with | StatusCode : {StatusCode}| Errors : {ErrorDetails} for Holdings Service.");
                        }

                        _logger.LogError(EventIds.GetHoldingsDataToHoldingsFailed.ToEventId(), "Failed to get holdings data | StatusCode : {StatusCode}", httpResponseMessage.StatusCode.ToString());
                        throw new Exception("Failed to get holding data");
                    }
            }
        }
    }
}
