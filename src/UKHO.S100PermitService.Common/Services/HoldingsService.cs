using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class HoldingsService : IHoldingsService
    {
        private readonly ILogger<HoldingsService> _logger;
        private readonly IOptions<HoldingsServiceApiConfiguration> _holdingsServiceApiConfiguration;
        private readonly IAuthHoldingsServiceTokenProvider _authHoldingsServiceTokenProvider;
        private readonly IHoldingsApiClient _holdingsApiClient;
        private const string HoldingsUrl = "/holdings/{0}/s100";

        public HoldingsService(ILogger<HoldingsService> logger, IOptions<HoldingsServiceApiConfiguration> holdingsApiConfiguration, IAuthHoldingsServiceTokenProvider authHoldingsServiceTokenProvider, IHoldingsApiClient holdingsApiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsServiceApiConfiguration = holdingsApiConfiguration ?? throw new ArgumentNullException(nameof(holdingsApiConfiguration));
            _authHoldingsServiceTokenProvider = authHoldingsServiceTokenProvider ?? throw new ArgumentNullException(nameof(authHoldingsServiceTokenProvider));
            _holdingsApiClient = holdingsApiClient ?? throw new ArgumentNullException(nameof(holdingsApiClient));
        }

        public async Task<List<HoldingsServiceResponse>> GetHoldingsAsync(int licenceId, string correlationId)
        {
            var uri = new Uri(_holdingsServiceApiConfiguration.Value.BaseUrl + string.Format(HoldingsUrl, licenceId));

            _logger.LogInformation(EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId(),
                "Request to HoldingsService GET {RequestUri} started.", uri);

            var accessToken = await _authHoldingsServiceTokenProvider.GetManagedIdentityAuthAsync(_holdingsServiceApiConfiguration.Value.ClientId);

            var httpResponseMessage = await _holdingsApiClient.GetHoldingsAsync(uri.AbsoluteUri, licenceId, accessToken, correlationId);

            if(httpResponseMessage.IsSuccessStatusCode)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                _logger.LogInformation(EventIds.HoldingsServiceGetHoldingsRequestCompleted.ToEventId(),
                    "Request to HoldingsService GET {RequestUri} completed. Status Code: {StatusCode}", uri,
                    httpResponseMessage.StatusCode.ToString());

                var holdingsServiceResponse = JsonConvert.DeserializeObject<List<HoldingsServiceResponse>>(bodyJson);
                return holdingsServiceResponse;
            }

            if(httpResponseMessage.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(),
                    "Request to HoldingsService GET {0} failed. Status Code: {1} | Error Details: {2}.",
                    uri, httpResponseMessage.StatusCode.ToString(), bodyJson);
            }

            throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(),
                "Request to HoldingsService GET {0} failed. Status Code: {1}.",
                uri, httpResponseMessage.StatusCode.ToString());
        }
    }
}
