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
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class ProductKeyService : IProductKeyService
    {
        private readonly ILogger<ProductKeyService> _logger;
        private readonly IOptions<ProductKeyServiceApiConfiguration> _productKeyServiceApiConfiguration;
        private readonly IProductKeyServiceAuthTokenProvider _productKeyServiceAuthTokenProvider;
        private readonly IProductKeyServiceApiClient _productKeyServiceApiClient;
        private readonly IWaitAndRetryPolicy _waitAndRetryPolicy;
        private readonly IUriFactory _uriFactory;

        private const string KeysEnc = "/keys/s100";

        public ProductKeyService(ILogger<ProductKeyService> logger, IOptions<ProductKeyServiceApiConfiguration> productKeyServiceApiConfiguration, IProductKeyServiceAuthTokenProvider productKeyServiceAuthTokenProvider, IProductKeyServiceApiClient productKeyServiceApiClient, IWaitAndRetryPolicy waitAndRetryPolicy, IUriFactory uriFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _productKeyServiceApiConfiguration = productKeyServiceApiConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceApiConfiguration));
            _productKeyServiceAuthTokenProvider = productKeyServiceAuthTokenProvider ?? throw new ArgumentNullException(nameof(productKeyServiceAuthTokenProvider));
            _productKeyServiceApiClient = productKeyServiceApiClient ?? throw new ArgumentNullException(nameof(productKeyServiceApiClient));
            _waitAndRetryPolicy = waitAndRetryPolicy ?? throw new ArgumentNullException(nameof(waitAndRetryPolicy));
            _uriFactory = uriFactory ?? throw new ArgumentNullException(nameof(uriFactory));
        }

        /// <summary>
        /// Get product keys from Product Key Service.
        /// </summary>
        /// <remarks>
        /// If service responded with 429 TooManyRequests or 503 ServiceUnavailable StatusCodes, Then re-try mechanism will be triggered.
        /// If service responded with other than 200 Ok StatusCode, Then PermitServiceException exception will be thrown.
        /// </remarks>
        /// <param name="productKeyServiceRequest">Product Key Service request body.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Product key details.</returns>
        /// <exception cref="PermitServiceException">PermitServiceException exception will be thrown when exception occurred.</exception>
        public async Task<ServiceResponseResult<IEnumerable<ProductKeyServiceResponse>>> GetProductKeysAsync(IEnumerable<ProductKeyServiceRequest> productKeyServiceRequest, string correlationId, CancellationToken cancellationToken)
        {
            var uri = _uriFactory.CreateUri(_productKeyServiceApiConfiguration.Value.BaseUrl, KeysEnc);

            _logger.LogInformation(EventIds.GetProductKeysRequestStarted.ToEventId(), "Request to ProductKeyService POST Uri : {RequestUri} started.", uri.AbsolutePath);

            var accessToken = ""; //await _productKeyServiceAuthTokenProvider.GetManagedIdentityAuthAsync(_productKeyServiceApiConfiguration.Value.ClientId);

            var httpResponseMessage = await _waitAndRetryPolicy.GetRetryPolicyAsync(_logger, EventIds.RetryHttpClientProductKeyServiceRequest).ExecuteAsync(() =>
                _productKeyServiceApiClient.GetProductKeysAsync(uri.AbsoluteUri, productKeyServiceRequest, accessToken, correlationId, cancellationToken)
            );

            var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

            if(httpResponseMessage.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.GetProductKeysRequestCompletedWithStatus200OK.ToEventId(), "Request to ProductKeyService POST Uri : {RequestUri} completed. | StatusCode : {StatusCode}", uri.AbsolutePath, httpResponseMessage.StatusCode);

                var productKeyServiceResponse = JsonSerializer.Deserialize<IEnumerable<ProductKeyServiceResponse>>(bodyJson)!;

                return ServiceResponseResult<IEnumerable<ProductKeyServiceResponse>>.Success(productKeyServiceResponse);
            }

            var origin = httpResponseMessage.Headers.Contains(PermitServiceConstants.OriginHeaderKey) ? httpResponseMessage.Headers.GetValues(PermitServiceConstants.OriginHeaderKey).FirstOrDefault() : PermitServiceConstants.PermitKeyService;
            var errorResponse = default(ErrorResponse);

            if(!string.IsNullOrEmpty(bodyJson))
            {
                errorResponse = JsonSerializer.Deserialize<ErrorResponse>(bodyJson)!;
            }

            _logger.LogInformation(EventIds.ProductKeyServiceGetProductKeysRequestFailed.ToEventId(), "Request to ProductKeyService POST Uri : {RequestUri} failed. | StatusCode : {StatusCode} | Error Details : {Errors}", uri.AbsolutePath, httpResponseMessage.StatusCode, bodyJson);

            return ServiceResponseResult<IEnumerable<ProductKeyServiceResponse>>.Failure(httpResponseMessage.StatusCode, new ErrorResponse()
            {
                Errors = errorResponse?.Errors!,
                CorrelationId = correlationId,
                Origin = origin!
            });
        }
    }
}