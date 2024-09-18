using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;

namespace UKHO.S100PermitService.API.FunctionalTests.Auth
{
    public class AuthTokenProvider : TestBase
    {
        private string? _token;
        private TokenConfiguration? _tokenConfiguration;

        public async Task<string> GetPermitServiceToken(string clientId, string clientSecret)
        {
            var serviceProvider = GetServiceProvider();
            _tokenConfiguration = serviceProvider?.GetRequiredService<IOptions<TokenConfiguration>>().Value;
            string[] scopes = [$"{clientId}/.default"];
            if(_token == null)
            {
                if(_tokenConfiguration!.IsRunningOnLocalMachine)
                {
                    var debugApp = PublicClientApplicationBuilder.Create(clientId).
                                                        WithRedirectUri("http://localhost").Build();

                    //Acquiring token through user interaction
                    var tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                                   .WithAuthority($"{_tokenConfiguration?.MicrosoftOnlineLoginUrl}{_tokenConfiguration?.TenantId}", true)
                                                                   .ExecuteAsync();
                    _token = tokenTask.AccessToken;
                }
                else
                {
                    var app = ConfidentialClientApplicationBuilder.Create(clientId)
                                                                  .WithClientSecret(clientSecret)
                                                                  .WithAuthority(new Uri($"{_tokenConfiguration?.MicrosoftOnlineLoginUrl}{_tokenConfiguration?.TenantId}"))
                                                                  .Build();

                    var tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    _token = tokenTask.AccessToken;
                }
            }
            return _token;
        }
    }
}