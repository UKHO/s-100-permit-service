using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;

namespace UKHO.S100PermitService.API.FunctionalTests.Auth
{
    public class AuthTokenProvider : TestBase
    {
        private TokenConfiguration? _tokenConfiguration;

        /// <summary>
        /// This method is used to generate the token
        /// </summary>
        /// <param name="clientId">Sets the Client ID to generate the token</param>
        /// <param name="clientSecret">Sets the Client Secret to generate the token</param>
        /// <returns></returns>
        public async Task<string> AsyncGetPermitServiceToken(string clientId, string clientSecret)
        {
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            string[] scopes = [$"{_tokenConfiguration?.ClientId}/.default"];
            string? token = null;
            if(token == null)
            {
                if(_tokenConfiguration!.IsRunningOnLocalMachine)
                {
                    var debugApp = PublicClientApplicationBuilder.Create(_tokenConfiguration?.ClientId).
                                                        WithRedirectUri("http://localhost").Build();

                    var tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                 .WithTenantId($"{_tokenConfiguration?.TenantId}")
                                                 .ExecuteAsync();
                    token = tokenTask.AccessToken;
                }
                else
                {
                    var app = ConfidentialClientApplicationBuilder.Create(clientId)
                                                                  .WithClientSecret(clientSecret)
                                                                  .WithAuthority(new Uri($"{_tokenConfiguration?.MicrosoftOnlineLoginUrl}{_tokenConfiguration?.TenantId}"))
                                                                  .Build();

                    var tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    token = tokenTask.AccessToken;
                }
            }
            return token;
        }
    }
}