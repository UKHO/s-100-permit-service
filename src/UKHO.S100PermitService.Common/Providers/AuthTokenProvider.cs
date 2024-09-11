using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Providers
{
    [ExcludeFromCodeCoverage]
    public class AuthTokenProvider : IAuthHoldingsServiceTokenProvider, IAuthUserPermitServiceTokenProvider, IAuthProductKeyServiceTokenProvider
    {
        private static readonly object _lock = new();
        private readonly IOptions<PermitServiceManagedIdentityConfiguration> _permitServiceManagedIdentityConfiguration;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<AuthTokenProvider> _logger;

        public AuthTokenProvider(IOptions<PermitServiceManagedIdentityConfiguration> permitServiceManagedIdentityConfiguration, IDistributedCache distributedCache, ILogger<AuthTokenProvider> logger)
        {
            _permitServiceManagedIdentityConfiguration = permitServiceManagedIdentityConfiguration ?? throw new ArgumentNullException(nameof(permitServiceManagedIdentityConfiguration));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetManagedIdentityAuthAsync(string resource)
        {
            _logger.LogInformation(EventIds.GetAccessTokenStarted.ToEventId(), "Getting access token to call external endpoint started | {DateTime}", DateTime.Now.ToUniversalTime());

            var authTokenFromCache = GetAuthTokenFromCache(resource);
            if(authTokenFromCache is { AccessToken: not null } && authTokenFromCache.ExpiresIn > DateTime.UtcNow)
            {
                _logger.LogInformation(EventIds.CachedAccessTokenFound.ToEventId(), "Valid access token found in cache to call external endpoint | {DateTime}", DateTime.Now.ToUniversalTime());
                return authTokenFromCache.AccessToken;
            }

            var newAuthToken = await GetAuthToken(resource);
            AddAuthTokenToCache(resource, newAuthToken);

            _logger.LogInformation(EventIds.GetAccessTokenCompleted.ToEventId(), "Getting access token to call external endpoint completed | {DateTime}", DateTime.Now.ToUniversalTime());

            return newAuthToken.AccessToken!;
        }

        private async Task<AuthTokenDetails> GetAuthToken(string resource)
        {
            _logger.LogInformation(EventIds.GetNewAccessTokenStarted.ToEventId(), "Generating new access token to call external endpoint started | {DateTime}", DateTime.Now.ToUniversalTime());

            var tokenCredential = new DefaultAzureCredential();
            var accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext(scopes: new string[] { resource + "/.default" }) { });

            _logger.LogInformation(EventIds.GetNewAccessTokenCompleted.ToEventId(), "New access token to call external endpoint generated successfully | {DateTime}", DateTime.Now.ToUniversalTime());

            return new AuthTokenDetails
            {
                ExpiresIn = accessToken.ExpiresOn.UtcDateTime,
                AccessToken = accessToken.Token
            };
        }

        private void AddAuthTokenToCache(string key, AuthTokenDetails authTokenDetails)
        {
            _logger.LogInformation(EventIds.CachingExternalEndPointTokenStarted.ToEventId(), "Adding new access token in cache to call external endpoint | {DateTime}", DateTime.Now.ToUniversalTime());

            var tokenExpiryMinutes = authTokenDetails.ExpiresIn.Subtract(DateTime.UtcNow).TotalMinutes;
            var deductTokenExpiryMinutes = _permitServiceManagedIdentityConfiguration.Value.DeductTokenExpiryMinutes < tokenExpiryMinutes ? _permitServiceManagedIdentityConfiguration.Value.DeductTokenExpiryMinutes : 1;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = authTokenDetails.ExpiresIn.Subtract(TimeSpan.FromMinutes(deductTokenExpiryMinutes))
            };

            lock(_lock)
            {
                _distributedCache.SetString(key, JsonSerializer.Serialize(authTokenDetails), options);
            }

            _logger.LogInformation(EventIds.CachingExternalEndPointTokenCompleted.ToEventId(), "New token is added in cache to call external endpoint and it expires in {ExpiresIn} with sliding expiration duration {options}.", Convert.ToString(authTokenDetails.ExpiresIn, CultureInfo.InvariantCulture), JsonSerializer.Serialize(options));
        }

        private AuthTokenDetails? GetAuthTokenFromCache(string key)
        {
            lock(_lock)
            {
                var item = _distributedCache.GetString(key);
                return item != null ? JsonSerializer.Deserialize<AuthTokenDetails>(item) : null;
            }
        }
    }
}