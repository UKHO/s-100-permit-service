using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Factories;
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
        private readonly IUriFactory _uriFactory;

        private const string HoldingsUrl = "/holdings/{0}/s100";

        public HoldingsService(ILogger<HoldingsService> logger, IOptions<HoldingsServiceApiConfiguration> holdingsApiConfiguration, IHoldingsServiceAuthTokenProvider holdingsServiceAuthTokenProvider, IHoldingsApiClient holdingsApiClient, IWaitAndRetryPolicy waitAndRetryPolicy, IUriFactory uriFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsServiceApiConfiguration = holdingsApiConfiguration ?? throw new ArgumentNullException(nameof(holdingsApiConfiguration));
            _holdingsServiceAuthTokenProvider = holdingsServiceAuthTokenProvider ?? throw new ArgumentNullException(nameof(holdingsServiceAuthTokenProvider));
            _holdingsApiClient = holdingsApiClient ?? throw new ArgumentNullException(nameof(holdingsApiClient));
            _waitAndRetryPolicy = waitAndRetryPolicy ?? throw new ArgumentNullException(nameof(waitAndRetryPolicy));
            _uriFactory = uriFactory ?? throw new ArgumentNullException(nameof(uriFactory));
        }

        public async Task<(HttpStatusCode httpStatusCode, IEnumerable<HoldingsServiceResponse>? holdingsServiceResponse)>
            GetHoldingsAsync(int licenceId, CancellationToken cancellationToken, string correlationId)
        {
            var uri = _uriFactory.CreateUri(_holdingsServiceApiConfiguration.Value.BaseUrl, HoldingsUrl, licenceId);

            _logger.LogInformation(EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId(),
                "Request to HoldingsService GET Uri : {RequestUri} started.", uri.AbsolutePath);

            var accessToken = await _holdingsServiceAuthTokenProvider.GetManagedIdentityAuthAsync(_holdingsServiceApiConfiguration.Value.ClientId);

            var httpResponseMessage = await _waitAndRetryPolicy.GetRetryPolicyAsync(_logger, EventIds.RetryHttpClientHoldingsRequest).ExecuteAsync(async () =>
            {
                return await _holdingsApiClient.GetHoldingsAsync(uri.AbsoluteUri, licenceId, accessToken, cancellationToken, correlationId);
            });

            return await HandleResponseAsync(httpResponseMessage, uri, cancellationToken);
        }

        public IEnumerable<HoldingsServiceResponse> FilterHoldingsByLatestExpiry(IEnumerable<HoldingsServiceResponse> holdingsServiceResponse)
        {
            var allCells = holdingsServiceResponse.SelectMany(p => p.Cells.Select(c => new { p.ProductCode, p.ProductTitle, p.ExpiryDate, Cell = c }));

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

            _logger.LogInformation(EventIds.HoldingsFilteredCellCount.ToEventId(), "Filtered holdings: Total count before filtering: {TotalCellCount}, after filtering for highest expiry dates and removing duplicates: {FilteredCellCount}.", allCells.Count(), latestCells.Count());

            return filteredHoldings;
        }

        private async Task<(HttpStatusCode httpStatusCode, List<HoldingsServiceResponse>? holdingsServiceResponse)> HandleResponseAsync(HttpResponseMessage httpResponseMessage, Uri uri, CancellationToken cancellationToken)
        {
            if(httpResponseMessage.IsSuccessStatusCode)
            {
                var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation(EventIds.HoldingsServiceGetHoldingsRequestCompleted.ToEventId(),
                    "Request to HoldingsService GET Uri : {RequestUri} completed. | StatusCode: {StatusCode}", uri.AbsolutePath,
                    httpResponseMessage.StatusCode.ToString());

                return (httpResponseMessage.StatusCode, JsonSerializer.Deserialize<List<HoldingsServiceResponse>>(bodyJson));
            }

            return await HandleErrorResponseAsync(httpResponseMessage, uri, cancellationToken);
        }

        private async Task<(HttpStatusCode httpStatusCode, List<HoldingsServiceResponse>? holdingsServiceResponse)> HandleErrorResponseAsync(HttpResponseMessage httpResponseMessage, Uri uri, CancellationToken cancellationToken)
        {
            var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

            if(httpResponseMessage.StatusCode is HttpStatusCode.BadRequest)
            {
                throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(),
                    "Request to HoldingsService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode} | Error Details: {Errors}",
                    uri.AbsolutePath, httpResponseMessage.StatusCode.ToString(), bodyJson);
            }

            if(httpResponseMessage.StatusCode is HttpStatusCode.NotFound)
            {
                _logger.LogError(EventIds.HoldingServiceGetHoldingsLicenceNotFound.ToEventId(),
                    "Request to HoldingsService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode} | Error Details: {Errors}",
                    uri.AbsolutePath, httpResponseMessage.StatusCode.ToString(), bodyJson);

                return (httpResponseMessage.StatusCode, null);
            }

            throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(),
                "Request to HoldingsService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode}",
                uri.AbsolutePath, httpResponseMessage.StatusCode.ToString());
        }
    }
}
