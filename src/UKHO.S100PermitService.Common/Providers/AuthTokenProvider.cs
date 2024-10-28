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

namespace UKHO.S100PermitService.Common.Providers
{
    [ExcludeFromCodeCoverage]
    public class AuthTokenProvider : IHoldingsServiceAuthTokenProvider, IUserPermitServiceAuthTokenProvider, IProductKeyServiceAuthTokenProvider
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
            _logger.LogInformation(EventIds.GetAccessTokenStarted.ToEventId(), "Getting access token to call external endpoint started.");

            var authTokenFromCache = GetAuthTokenFromCache(resource);
            if(authTokenFromCache is { AccessToken: not null } && authTokenFromCache.ExpiresIn > DateTime.UtcNow)
            {
                _logger.LogInformation(EventIds.CachedAccessTokenFound.ToEventId(), "Valid access token found in cache to call external endpoint.");
                return authTokenFromCache.AccessToken;
            }

            var newAuthToken = await GetAuthToken(resource);
            AddAuthTokenToCache(resource, newAuthToken);

            _logger.LogInformation(EventIds.GetAccessTokenCompleted.ToEventId(), "Getting access token to call external endpoint completed.");

            return newAuthToken.AccessToken!;
        }

        private async Task<AuthToken> GetAuthToken(string resource)
        {
            _logger.LogInformation(EventIds.GetNewAccessTokenStarted.ToEventId(), "Generating new access token to call external endpoint started.");

            var tokenCredential = new DefaultAzureCredential();
            var accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext(scopes: new string[] { resource + "/.default" }) { });

            _logger.LogInformation(EventIds.GetNewAccessTokenCompleted.ToEventId(), "New access token to call external endpoint generated successfully.");

            return new AuthToken
            {
                ExpiresIn = accessToken.ExpiresOn.UtcDateTime,
                AccessToken = accessToken.Token
            };
        }

        private void AddAuthTokenToCache(string key, AuthToken authTokenDetails)
        {
            _logger.LogInformation(EventIds.CachingExternalEndPointTokenStarted.ToEventId(), "Adding new access token in cache to call external endpoint.");

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

        private AuthToken? GetAuthTokenFromCache(string key)
        {
            lock(_lock)
            {
                var item = _distributedCache.GetString(key);
                return item != null ? JsonSerializer.Deserialize<AuthToken>(item) : null;
            }
        }
    }
}