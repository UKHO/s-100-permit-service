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
using UKHO.S100PermitService.Common.Models;
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

        /// <summary>
        /// Get Holding details from Shop Facade - Holding Service for requested licence id.
        /// </summary>
        /// <remarks>
        /// If invalid or non exists licence id requested, Then status code 404 NotFound will be returned.
        /// If service responded with 429 TooManyRequests or 503 ServiceUnavailable StatusCodes, Then re-try mechanism will be triggered.
        /// If service responded with other than 200 Ok or 404 NotFound StatusCodes, Then PermitServiceException exception will be thrown.
        /// </remarks>
        /// <param name="licenceId">Requested licence id.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <response code="200">Holding details.</response>
        /// <response code="204">NoContent - when service returned with empty response.</response>
        /// <response code="404">NotFound - when invalid or non exists licence Id requested.</response>
        /// <exception cref="PermitServiceException">PermitServiceException exception will be thrown when exception occurred or status code other than 200 OK and 404 NotFound returned.</exception>
        public async Task<ServiceResponseResult<List<HoldingsServiceResponse>>> GetHoldingsAsync(int licenceId, string correlationId, CancellationToken cancellationToken)
        {
            var uri = _uriFactory.CreateUri(_holdingsServiceApiConfiguration.Value.BaseUrl, HoldingsUrl, licenceId);

            _logger.LogInformation(EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId(),
                "Request to HoldingsService GET Uri : {RequestUri} started.", uri.AbsolutePath);

            var accessToken = await _holdingsServiceAuthTokenProvider.GetManagedIdentityAuthAsync(_holdingsServiceApiConfiguration.Value.ClientId);

            var httpResponseMessage = await _waitAndRetryPolicy.GetRetryPolicyAsync(_logger, EventIds.RetryHttpClientHoldingsRequest).ExecuteAsync(async () =>
            {
                return await _holdingsApiClient.GetHoldingsAsync(uri.AbsoluteUri, licenceId, accessToken, correlationId, cancellationToken);
            });

            return await HandleResponseAsync(httpResponseMessage, uri, cancellationToken);
        }

        /// <summary>
        /// Remove duplicate dataset and select the dataset with highest expiry date.
        /// </summary>
        /// <param name="holdingsServiceResponse">Holding details.</param>
        /// <returns>Filtered holding details.</returns>
        public IEnumerable<HoldingsServiceResponse> FilterHoldingsByLatestExpiry(IEnumerable<HoldingsServiceResponse> holdingsServiceResponse)
        {
            var allCells = holdingsServiceResponse.SelectMany(p => p.Datasets.Select(d => new { p.UnitName, p.UnitTitle, p.ExpiryDate, DataSet = d }));

            var latestCells = allCells
                .GroupBy(c => c.DataSet.DatasetName)
                .Select(g => g.OrderByDescending(c => c.ExpiryDate).First());

            var filteredHoldings = latestCells
                .GroupBy(c => new { c.UnitName, c.UnitTitle })
                .Select(g => new HoldingsServiceResponse
                {
                    UnitName = g.Key.UnitName,
                    UnitTitle = g.Key.UnitTitle,
                    ExpiryDate = g.Max(c => c.ExpiryDate),
                    Datasets = g.Select(c => c.DataSet).ToList()
                }).ToList();

            _logger.LogInformation(EventIds.HoldingsFilteredCellCount.ToEventId(), "Filtered holdings: Total count before filtering: {TotalCellCount}, after filtering for highest expiry dates and removing duplicates: {FilteredCellCount}.", allCells.Count(), latestCells.Count());

            return filteredHoldings;
        }

        private async Task<ServiceResponseResult<List<HoldingsServiceResponse>>> HandleResponseAsync(HttpResponseMessage httpResponseMessage, Uri uri, CancellationToken cancellationToken)
        {
            if(httpResponseMessage.IsSuccessStatusCode)
            {
                var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

                if(httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogInformation(EventIds.HoldingsServiceGetHoldingsRequestCompletedWithStatus200OK.ToEventId(), "Request to HoldingsService GET Uri : {RequestUri} completed. | StatusCode: {StatusCode}", uri.AbsolutePath, httpResponseMessage.StatusCode);

                    var response = JsonSerializer.Deserialize<List<HoldingsServiceResponse>>(bodyJson);
                    return ServiceResponseResult<List<HoldingsServiceResponse>>.Success(response);
                }

                if(httpResponseMessage.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogWarning(EventIds.HoldingsServiceGetHoldingsRequestCompletedWithStatus204NoContent.ToEventId(), "Request to HoldingsService GET Uri : {RequestUri} completed. | StatusCode: {StatusCode} | ResponseMessage: {ResponseMessage}", uri.AbsolutePath, httpResponseMessage.StatusCode, bodyJson);

                    return ServiceResponseResult<List<HoldingsServiceResponse>>.NoContent();
                }
            }

            return await HandleNonSuccessResponseAsync(httpResponseMessage, uri, cancellationToken);
        }

        private async Task<ServiceResponseResult<List<HoldingsServiceResponse>>> HandleNonSuccessResponseAsync(HttpResponseMessage httpResponseMessage, Uri uri, CancellationToken cancellationToken)
        {
            var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

            if(httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogWarning(EventIds.HoldingsServiceGetHoldingsRequestCompletedWithStatus400BadRequest.ToEventId(), "Request to HoldingsService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode} | ResponseMessage: {ResponseMessage}", uri.AbsolutePath, httpResponseMessage.StatusCode, bodyJson);

                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(bodyJson);
                return ServiceResponseResult<List<HoldingsServiceResponse>>.BadRequest(errorResponse);
            }

            if(httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(EventIds.HoldingsServiceGetHoldingsRequestCompletedWithStatus404NotFound.ToEventId(), "Request to HoldingsService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode} | ResponseMessage: {ResponseMessage}", uri.AbsolutePath, httpResponseMessage.StatusCode, bodyJson);

                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(bodyJson);
                return ServiceResponseResult<List<HoldingsServiceResponse>>.NotFound(errorResponse);
            }

            throw new PermitServiceException(EventIds.HoldingsServiceGetHoldingsRequestFailed.ToEventId(), "Request to HoldingsService POST Uri : {RequestUri} failed. | StatusCode : {StatusCode} | Error Details : {Errors}", uri.AbsolutePath, httpResponseMessage.StatusCode, bodyJson);
        }
    }
}