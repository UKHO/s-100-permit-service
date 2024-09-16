using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;

namespace UKHO.S100PermitService.API.FunctionalTests.Helpers
{
    public class AuthTokenProvider : TestBase
    {
        private string? _token = null;
        private TokenConfiguration? _tokenConfiguration;

        public async Task<string> GetPSToken(string clientId, string clientSecret)
        {
            return await GetPermitServiceToken(clientId, clientSecret);
        }

        public async Task<string> GetPermitServiceToken(string clientId, string clientSecret)
        {
            var _serviceProvider = GetServiceProvider();
            _tokenConfiguration = _serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            string[] scopes = [$"{clientId}/.default"];
            if(_token == null)
            {
                if(_tokenConfiguration!.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(clientId).
                                                        WithRedirectUri("http://localhost").Build();

                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                                   .WithAuthority($"{_tokenConfiguration?.MicrosoftOnlineLoginUrl}{_tokenConfiguration?.TenantId}", true)
                                                                   .ExecuteAsync();
                    _token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                                                                                             .WithClientSecret(clientSecret)
                                                                                             .WithAuthority(new Uri($"{_tokenConfiguration?.MicrosoftOnlineLoginUrl}{_tokenConfiguration?.TenantId}"))
                                                                                             .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    _token = tokenTask.AccessToken;
                }
            }
            return _token;
        }
    }
}