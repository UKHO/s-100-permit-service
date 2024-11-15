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
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.Services
{
    public class UserPermitService : IUserPermitService
    {
        private readonly ILogger<UserPermitService> _logger;
        private readonly IOptions<UserPermitServiceApiConfiguration> _userPermitServiceApiConfiguration;
        private readonly IUserPermitServiceAuthTokenProvider _userPermitServiceAuthTokenProvider;
        private readonly IUserPermitApiClient _userPermitApiClient;
        private readonly IWaitAndRetryPolicy _waitAndRetryPolicy;
        private readonly IUserPermitValidator _userPermitValidator;
        private readonly IUriFactory _uriFactory;

        private const string UserPermitUrl = "/userpermits/{0}/s100";

        public UserPermitService(ILogger<UserPermitService> logger,
            IOptions<UserPermitServiceApiConfiguration> userPermitServiceApiConfiguration,
            IUserPermitServiceAuthTokenProvider userPermitServiceAuthTokenProvider,
            IUserPermitApiClient userPermitApiClient,
            IWaitAndRetryPolicy waitAndRetryPolicy,
            IUserPermitValidator userPermitValidator,
            IUriFactory uriFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userPermitServiceApiConfiguration = userPermitServiceApiConfiguration ??
                                                 throw new ArgumentNullException(
                                                     nameof(userPermitServiceApiConfiguration));
            _userPermitServiceAuthTokenProvider = userPermitServiceAuthTokenProvider ??
                                                  throw new ArgumentNullException(
                                                      nameof(userPermitServiceAuthTokenProvider));
            _userPermitApiClient = userPermitApiClient ?? throw new ArgumentNullException(nameof(userPermitApiClient));
            _waitAndRetryPolicy = waitAndRetryPolicy ?? throw new ArgumentNullException(nameof(waitAndRetryPolicy));
            _userPermitValidator = userPermitValidator ?? throw new ArgumentNullException(nameof(userPermitValidator));
            _uriFactory = uriFactory ?? throw new ArgumentNullException(nameof(uriFactory));
        }

        /// <summary>
        /// Get User Permit Number (UPN) details from Shop Facade - User Permit Service for requested licence id.
        /// </summary>
        /// <remarks>
        /// If invalid or non exists licence id requested, Then status code 404 NotFound will be returned.
        /// If service responded with 429 TooManyRequests or 503 ServiceUnavailable StatusCodes, Then re-try mechanism will be triggered.
        /// If service responded with other than 200 Ok or 404 NotFound StatusCodes, Then PermitServiceException exception will be thrown.
        /// </remarks>
        /// <param name="licenceId">Requested licence id.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <response code="200">User Permit Number (UPN) details.</response>
        /// <response code="404">NotFound - when invalid or non exists licence Id requested.</response>
        /// <exception cref="PermitServiceException">PermitServiceException exception will be thrown when exception occurred or status code other than 200 OK and 404 NotFound returned.</exception>
        public async Task<ServiceResponseResult<UserPermitServiceResponse>> GetUserPermitAsync(int licenceId, CancellationToken cancellationToken, string correlationId)
        {
            var uri = _uriFactory.CreateUri(_userPermitServiceApiConfiguration.Value.BaseUrl, UserPermitUrl, licenceId);

            _logger.LogInformation(EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId(), "Request to UserPermitService GET Uri : {RequestUri} started.", uri.AbsolutePath);

            var accessToken = await _userPermitServiceAuthTokenProvider.GetManagedIdentityAuthAsync(_userPermitServiceApiConfiguration.Value.ClientId);

            var httpResponseMessage = await _waitAndRetryPolicy
                .GetRetryPolicyAsync(_logger, EventIds.RetryHttpClientUserPermitRequest).ExecuteAsync(async () =>
                {
                    return await _userPermitApiClient.GetUserPermitsAsync(uri.AbsoluteUri, licenceId, accessToken,
                        cancellationToken, correlationId);
                });

            return await HandleResponseAsync(httpResponseMessage, uri, cancellationToken);
        }

        private async Task<ServiceResponseResult<UserPermitServiceResponse>>
            HandleResponseAsync(HttpResponseMessage httpResponseMessage, Uri uri, CancellationToken cancellationToken)
        {
            var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

            if(httpResponseMessage.IsSuccessStatusCode)
            {
                if(httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var response = JsonSerializer.Deserialize<UserPermitServiceResponse>(bodyJson);

                    _logger.LogInformation(EventIds.UserPermitServiceGetUserPermitsRequestCompleted.ToEventId(), "Request to UserPermitService GET Uri : {RequestUri} completed. | StatusCode: {StatusCode}", uri.AbsolutePath, httpResponseMessage.StatusCode);

                    return ServiceResponseResult<UserPermitServiceResponse>.Success(response);
                }

                if(httpResponseMessage.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogWarning(EventIds.UserPermitServiceGetUserPermitsRequestCompletedWithNoContent.ToEventId(), "Request to UserPermitService responded with empty response.");

                    return ServiceResponseResult<UserPermitServiceResponse>.NoContent();
                }
            }

            return await HandleNonSuccessResponseAsync(httpResponseMessage, uri, cancellationToken);
        }

        private async Task<ServiceResponseResult<UserPermitServiceResponse>>
            HandleNonSuccessResponseAsync(HttpResponseMessage httpResponseMessage, Uri uri, CancellationToken cancellationToken)
        {
            var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

            if(httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(bodyJson);
                //TODO: add / update event id
                _logger.LogWarning(EventIds.UserPermitServiceGetUserPermitsLicenceNotFound.ToEventId(),
                "Request to UserPermitService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode} | Error Details: {Errors}",
                uri.AbsolutePath, httpResponseMessage.StatusCode, bodyJson);

                return ServiceResponseResult<UserPermitServiceResponse>.BadRequest(errorResponse);
            }

            if(httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(bodyJson);

                _logger.LogWarning(EventIds.UserPermitServiceGetUserPermitsLicenceNotFound.ToEventId(),
                "Request to UserPermitService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode} | Error Details: {Errors}",
                uri.AbsolutePath, httpResponseMessage.StatusCode, bodyJson);

                return ServiceResponseResult<UserPermitServiceResponse>.NotFound(errorResponse);
            }

            throw new PermitServiceException(EventIds.UserPermitServiceGetUserPermitsRequestFailed.ToEventId(),
                "Request to UserPermitService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode}",
                uri.AbsolutePath, httpResponseMessage.StatusCode);
        }

        /// <summary>
        /// Validate User Permit Number (UPN) for any validation failures.
        /// </summary>
        /// <param name="userPermitServiceResponse">User Permit Number (UPN) details.</param>
        /// <exception cref="PermitServiceException">When validation failed then PermitServiceException exception will be thrown.</exception>
        public void ValidateUpnsAndChecksum(UserPermitServiceResponse userPermitServiceResponse)
        {
            var result = _userPermitValidator.Validate(userPermitServiceResponse);

            if(!result.IsValid)
            {
                var errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));

                throw new PermitServiceException(EventIds.UpnLengthOrChecksumValidationFailed.ToEventId(),
                    "Validation failed for Licence Id: {licenceId} | Error Details: {errorMessage}", userPermitServiceResponse.LicenceId, errorMessage);
            }
        }
    }
}