using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Handlers;
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
        private const string KeysEnc = "/keys/s100";

        public ProductKeyService(ILogger<ProductKeyService> logger, IOptions<ProductKeyServiceApiConfiguration> productKeyServiceApiConfiguration, IProductKeyServiceAuthTokenProvider productKeyServiceAuthTokenProvider, IProductKeyServiceApiClient productKeyServiceApiClient, IWaitAndRetryPolicy waitAndRetryPolicy)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _productKeyServiceApiConfiguration = productKeyServiceApiConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceApiConfiguration));
            _productKeyServiceAuthTokenProvider = productKeyServiceAuthTokenProvider ?? throw new ArgumentNullException(nameof(productKeyServiceAuthTokenProvider));
            _productKeyServiceApiClient = productKeyServiceApiClient ?? throw new ArgumentNullException(nameof(productKeyServiceApiClient));
            _waitAndRetryPolicy = waitAndRetryPolicy ?? throw new ArgumentNullException(nameof(waitAndRetryPolicy));
        }

        /// <summary>
        /// Get product keys from Product Key Service.
        /// </summary>
        /// <remarks>
        /// If service responded with 429 TooManyRequests or 503 ServiceUnavailable StatusCodes, Then re-try mechanism will be triggered.
        /// If service responded with other than 200 Ok StatusCode, Then PermitServiceException exception handler triggered.
        /// </remarks>
        /// <param name="productKeyServiceRequest">Product Key Service request body.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <returns>Product key details.</returns>
        /// <exception cref="PermitServiceException">PermitServiceException exception handler triggered when exception occurred.</exception>
        public async Task<List<ProductKeyServiceResponse>> GetProductKeysAsync(List<ProductKeyServiceRequest> productKeyServiceRequest, CancellationToken cancellationToken, string correlationId)
        {
            var uri = new Uri(_productKeyServiceApiConfiguration.Value.BaseUrl + KeysEnc);

            _logger.LogInformation(EventIds.GetProductKeysRequestStarted.ToEventId(), "Request to ProductKeyService POST Uri : {RequestUri} started.", uri.AbsolutePath);

            var accessToken = await _productKeyServiceAuthTokenProvider.GetManagedIdentityAuthAsync(_productKeyServiceApiConfiguration.Value.ClientId);

            var httpResponseMessage = _waitAndRetryPolicy.GetRetryPolicy(_logger, EventIds.RetryHttpClientProductKeyServiceRequest).Execute(() =>
            {
                return _productKeyServiceApiClient.GetProductKeysAsync(uri.AbsoluteUri, productKeyServiceRequest, accessToken, cancellationToken, correlationId).Result;
            });

            if(httpResponseMessage.IsSuccessStatusCode)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

                _logger.LogInformation(EventIds.GetProductKeysRequestCompleted.ToEventId(), "Request to ProductKeyService POST Uri : {RequestUri} completed. | StatusCode : {StatusCode}", uri.AbsolutePath, httpResponseMessage.StatusCode.ToString());

                var productKeyServiceResponse = JsonSerializer.Deserialize<List<ProductKeyServiceResponse>>(bodyJson)!;
                return productKeyServiceResponse;
            }

            if(httpResponseMessage.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
            {
                var bodyJson = httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

                throw new PermitServiceException(EventIds.GetProductKeysRequestFailed.ToEventId(),
                    "Request to ProductKeyService POST Uri : {RequestUri} failed. | StatusCode : {StatusCode} | Error Details : {Errors}",
                    uri.AbsolutePath, httpResponseMessage.StatusCode.ToString(), bodyJson);
            }

            throw new PermitServiceException(EventIds.GetProductKeysRequestFailed.ToEventId(),
                "Request to ProductKeyService POST Uri : {RequestUri} failed. | StatusCode : {StatusCode}",
                uri.AbsolutePath, httpResponseMessage.StatusCode.ToString());
        }
    }
}