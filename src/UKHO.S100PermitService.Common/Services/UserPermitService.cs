﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class UserPermitService : IUserPermitService
    {
        private readonly ILogger<UserPermitService> _logger;
        private readonly IOptions<UserPermitServiceApiConfiguration> _userPermitServiceApiConfiguration;
        private readonly IUserPermitServiceAuthTokenProvider _userPermitServiceAuthTokenProvider;
        private readonly IUserPermitApiClient _userPermitApiClient;
        private const string UserPermitUrl = "/userpermits/{0}/s100";

        public UserPermitService(ILogger<UserPermitService> logger, IOptions<UserPermitServiceApiConfiguration> userPermitServiceApiConfiguration, IUserPermitServiceAuthTokenProvider userPermitServiceAuthTokenProvider, IUserPermitApiClient userPermitApiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userPermitServiceApiConfiguration = userPermitServiceApiConfiguration ?? throw new ArgumentNullException(nameof(userPermitServiceApiConfiguration));
            _userPermitServiceAuthTokenProvider = userPermitServiceAuthTokenProvider ?? throw new ArgumentNullException(nameof(userPermitServiceAuthTokenProvider));
            _userPermitApiClient = userPermitApiClient ?? throw new ArgumentNullException(nameof(userPermitApiClient));
        }

        public async Task<UserPermitServiceResponse> GetUserPermitAsync(int licenceId, string correlationId)
        {
            string bodyJson;
            var requestUri = _userPermitServiceApiConfiguration.Value.BaseUrl + string.Format(UserPermitUrl, licenceId);
            var accessToken = await _userPermitServiceAuthTokenProvider.GetManagedIdentityAuthAsync(_userPermitServiceApiConfiguration.Value.ClientId);

            _logger.LogInformation(EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId(), "Request to UserPermitService GET {RequestUri} started", requestUri);

            var httpResponseMessage = await _userPermitApiClient.GetUserPermitsAsync(requestUri, licenceId, accessToken, correlationId);

            switch(httpResponseMessage.IsSuccessStatusCode)
            {
                case true:
                    {
                        bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        _logger.LogInformation(EventIds.UserPermitServiceGetUserPermitsRequestCompleted.ToEventId(), "Request to UserPermitService GET {RequestUri} completed. StatusCode: {StatusCode}", requestUri, httpResponseMessage.StatusCode.ToString());

                        var userPermitServiceResponse = JsonConvert.DeserializeObject<UserPermitServiceResponse>(bodyJson);

                        return userPermitServiceResponse;
                    }
                default:
                    {
                        if(httpResponseMessage.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
                        {
                            bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                            _logger.LogError(EventIds.UserPermitServiceGetUserPermitsRequestFailed.ToEventId(), "Request to UserPermitService GET {RequestUri} failed. StatusCode: {StatusCode} | Errors Details: {ErrorDetails}", requestUri, httpResponseMessage.StatusCode.ToString(), bodyJson);
                            throw new Exception();
                        }

                        _logger.LogError(EventIds.UserPermitServiceGetUserPermitsRequestFailed.ToEventId(), "Request to UserPermitService GET {RequestUri} failed. StatusCode: {StatusCode}", requestUri, httpResponseMessage.StatusCode.ToString());
                        throw new Exception();
                    }
            }
        }
    }
}