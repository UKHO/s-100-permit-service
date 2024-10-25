using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
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

        public async Task<(HttpStatusCode httpStatusCode, List<HoldingsServiceResponse>? holdingsServiceResponse)> GetHoldingsAsync(int licenceId, CancellationToken cancellationToken, string correlationId)
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

                var holdingsServiceResponse = JsonSerializer.Deserialize<List<HoldingsServiceResponse>>(bodyJson);
                return (httpResponseMessage.StatusCode, holdingsServiceResponse);
            }

            if(httpResponseMessage.StatusCode is HttpStatusCode.BadRequest)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(),
                    "Request to HoldingsService GET {RequestUri} failed. Status Code: {StatusCode} | Error Details: {Errors}.",
                    uri.AbsolutePath, httpResponseMessage.StatusCode.ToString(), bodyJson);
            }
            else if(httpResponseMessage.StatusCode is HttpStatusCode.NotFound)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                _logger.LogError(EventIds.HoldingServiceGetHoldingsLicenceNotFound.ToEventId(),
                    "Request to HoldingsService GET {RequestUri} failed. StatusCode: {StatusCode} | Errors Details: {Errors}",
                    uri.AbsolutePath, httpResponseMessage.StatusCode.ToString(), bodyJson);

                return (httpResponseMessage.StatusCode, null);
            }

            throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(),
                "Request to HoldingsService GET {RequestUri} failed. Status Code: {StatusCode}.",
                uri.AbsolutePath, httpResponseMessage.StatusCode.ToString());
        }

        public IEnumerable<HoldingsServiceResponse> FilterHoldingsByLatestExpiry(IEnumerable<HoldingsServiceResponse> holdingsServiceResponse)
        {
            var allCells = holdingsServiceResponse.SelectMany(p => p.Cells.Select(c => new { p.ProductCode, p.ProductTitle, p.ExpiryDate, Cell = c }));

            _logger.LogInformation(EventIds.HoldingsCellCount.ToEventId(), "Holdings total cell count : {Count}", allCells.Count());

            var latestCells = allCells
                .GroupBy(c => c.Cell.CellCode)
                .Select(g => g.OrderByDescending(c => c.ExpiryDate).First());

            var filteredHoldings = latestCells
                .GroupBy(c => new { c.ProductCode, c.ProductTitle })
                .Select(g => new HoldingsServiceResponse
                {
                    ProductCode = g.Key.ProductCode,
                    ProductTitle = g.Key.ProductTitle,
                    ExpiryDate = g.Max(c => c.ExpiryDate),
                    Cells = g.Select(c => c.Cell).ToList()
                }).ToList();

            _logger.LogInformation(EventIds.HoldingsFilteredCellCount.ToEventId(), "Holdings filtered cell count : {Count}", latestCells.Count());

            return filteredHoldings;
        }
    }
}
