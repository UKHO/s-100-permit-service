using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class UserPermitService : IUserPermitService
    {
        private readonly ILogger<UserPermitService> _logger;
        private readonly IOptions<UserPermitServiceApiConfiguration> _userPermitServiceApiConfiguration;
        private readonly IAuthUserPermitServiceTokenProvider _authUserPermitServiceTokenProvider;
        private readonly IUserPermitApiClient _userPermitApiClient;
        private const string UserPermitUrl = "/userpermits/{0}/s100";

        public UserPermitService(ILogger<UserPermitService> logger, IOptions<UserPermitServiceApiConfiguration> userPermitServiceApiConfiguration, IAuthUserPermitServiceTokenProvider authUserPermitServiceTokenProvider, IUserPermitApiClient userPermitApiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userPermitServiceApiConfiguration = userPermitServiceApiConfiguration ?? throw new ArgumentNullException(nameof(userPermitServiceApiConfiguration));
            _authUserPermitServiceTokenProvider = authUserPermitServiceTokenProvider ?? throw new ArgumentNullException(nameof(authUserPermitServiceTokenProvider));
            _userPermitApiClient = userPermitApiClient ?? throw new ArgumentNullException(nameof(userPermitApiClient));
        }

        public async Task<UserPermitServiceResponse> GetUserPermitAsync(int licenceId)
        {
            _logger.LogInformation(EventIds.GetUserPermitStarted.ToEventId(), "Request to get user permits from UserPermitService started");

            string bodyJson;
            string uri = _userPermitServiceApiConfiguration.Value.BaseUrl + string.Format(UserPermitUrl, licenceId);
            string accessToken = await _authUserPermitServiceTokenProvider.GetManagedIdentityAuthAsync(_userPermitServiceApiConfiguration.Value.ClientId);

            HttpResponseMessage httpResponseMessage = await _userPermitApiClient.GetUserPermitsAsync(uri, licenceId, accessToken);

            switch(httpResponseMessage.IsSuccessStatusCode)
            {
                case true:
                    {
                        bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        _logger.LogInformation(EventIds.GetUserPermitCompleted.ToEventId(), "Request to get user permits from UserPermitService  completed | StatusCode : {StatusCode}", httpResponseMessage.StatusCode.ToString());

                        UserPermitServiceResponse userPermitServiceResponse = JsonConvert.DeserializeObject<UserPermitServiceResponse>(bodyJson);

                        return userPermitServiceResponse;
                    }
                default:
                    {
                        if(httpResponseMessage.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                        {
                            bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                            _logger.LogError(EventIds.GetUserPermitException.ToEventId(), "Failed to retrieve user permits from UserPermitService | StatusCode : {StatusCode}| Errors : {ErrorDetails}", httpResponseMessage.StatusCode.ToString(), bodyJson);
                            throw new Exception();
                        }

                        _logger.LogError(EventIds.GetUserPermitException.ToEventId(), "Failed to retrieve user permits from UserPermitService | StatusCode : {StatusCode}", httpResponseMessage.StatusCode.ToString());
                        throw new Exception();
                    }
            }
        }
    }
}