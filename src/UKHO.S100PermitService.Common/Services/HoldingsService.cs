using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Handlers;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class HoldingsService : IHoldingsService
    {
        private readonly ILogger<HoldingsService> _logger;
        private readonly IOptions<HoldingsServiceApiConfiguration> _holdingsServiceApiConfiguration;
        private readonly IHoldingsServiceAuthTokenProvider _holdingsServiceAuthTokenProvider;
        private readonly IHoldingsApiClient _holdingsApiClient;
        private readonly IWaitAndRetryPolicy _waitAndRetryPolicy;
        private const string HoldingsUrl = "/holdings/{0}/s100";

        public HoldingsService(ILogger<HoldingsService> logger, IOptions<HoldingsServiceApiConfiguration> holdingsApiConfiguration, IHoldingsServiceAuthTokenProvider holdingsServiceAuthTokenProvider, IHoldingsApiClient holdingsApiClient, IWaitAndRetryPolicy waitAndRetryPolicy)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsServiceApiConfiguration = holdingsApiConfiguration ?? throw new ArgumentNullException(nameof(holdingsApiConfiguration));
            _holdingsServiceAuthTokenProvider = holdingsServiceAuthTokenProvider ?? throw new ArgumentNullException(nameof(holdingsServiceAuthTokenProvider));
            _holdingsApiClient = holdingsApiClient ?? throw new ArgumentNullException(nameof(holdingsApiClient));
            _waitAndRetryPolicy = waitAndRetryPolicy ?? throw new ArgumentNullException(nameof(waitAndRetryPolicy));
        }

        public async Task<List<HoldingsServiceResponse>> GetHoldingsAsync(int licenceId, CancellationToken cancellationToken, string correlationId)
        {
            var uri = new Uri(new Uri(_holdingsServiceApiConfiguration.Value.BaseUrl), string.Format(HoldingsUrl, licenceId));

            _logger.LogInformation(EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId(),
                "Request to HoldingsService GET {RequestUri} started.", uri.AbsolutePath);

            var accessToken = await _holdingsServiceAuthTokenProvider.GetManagedIdentityAuthAsync(_holdingsServiceApiConfiguration.Value.ClientId);

            var httpResponseMessage = _waitAndRetryPolicy.GetRetryPolicy(_logger, EventIds.RetryHttpClientHoldingsRequest).Execute(() =>
            {
                return _holdingsApiClient.GetHoldingsAsync(uri.AbsoluteUri, licenceId, accessToken, cancellationToken, correlationId).Result;
            });

            if(httpResponseMessage.IsSuccessStatusCode)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                _logger.LogInformation(EventIds.HoldingsServiceGetHoldingsRequestCompleted.ToEventId(),
                    "Request to HoldingsService GET {RequestUri} completed. Status Code: {StatusCode}", uri.AbsolutePath,
                    httpResponseMessage.StatusCode.ToString());

                var holdingsServiceResponse = JsonConvert.DeserializeObject<List<HoldingsServiceResponse>>(bodyJson);
                return holdingsServiceResponse;
            }

            if(httpResponseMessage.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(),
                    "Request to HoldingsService GET {0} failed. Status Code: {1} | Error Details: {2}.",
                    uri.AbsolutePath, httpResponseMessage.StatusCode.ToString(), bodyJson);
            }

            throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(),
                "Request to HoldingsService GET {0} failed. Status Code: {1}.",
                uri.AbsolutePath, httpResponseMessage.StatusCode.ToString());
        }
    }
}
