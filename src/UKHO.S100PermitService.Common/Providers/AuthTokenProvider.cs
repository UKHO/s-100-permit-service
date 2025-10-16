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
    public class AuthTokenProvider(
        IOptions<PermitServiceManagedIdentityConfiguration> permitServiceManagedIdentityConfiguration,
        IDistributedCache distributedCache,
        ILogger<AuthTokenProvider> logger,
        TokenCredential tokenCredential) : IProductKeyServiceAuthTokenProvider
    {
        private static readonly object _lock = new();
        private readonly IOptions<PermitServiceManagedIdentityConfiguration> _permitServiceManagedIdentityConfiguration = permitServiceManagedIdentityConfiguration ?? throw new ArgumentNullException(nameof(permitServiceManagedIdentityConfiguration));
        private readonly IDistributedCache _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        private readonly ILogger<AuthTokenProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TokenCredential _tokenCredential = tokenCredential;

        /// <summary>
        /// Generate managed identity authorization token and add into cache.
        /// </summary>
        /// <param name="resource">Client Id.</param>
        /// <returns>Authorization Token.</returns>
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

        /// <summary>
        /// Generate new authorization token
        /// </summary>
        /// <param name="resource">Client Id.</param>
        /// <returns>Authorization Token details.</returns>
        private async Task<AuthToken> GetAuthToken(string resource)
        {
            _logger.LogInformation(EventIds.GetNewAccessTokenStarted.ToEventId(), "Generating new access token to call external endpoint started.");

            var accessToken = await _tokenCredential.GetTokenAsync(new TokenRequestContext(scopes: new string[] { resource + "/.default" }) { }, new CancellationToken());

            _logger.LogInformation(EventIds.GetNewAccessTokenCompleted.ToEventId(), "New access token to call external endpoint generated successfully.");

            return new AuthToken
            {
                ExpiresIn = accessToken.ExpiresOn.UtcDateTime,
                AccessToken = accessToken.Token
            };
        }

        /// <summary>
        /// Add authorization token in cache.
        /// </summary>
        /// <param name="key">Client Id.</param>
        /// <param name="authTokenDetails">Authorization Token Details.</param>
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

            _logger.LogInformation(EventIds.CachingExternalEndPointTokenCompleted.ToEventId(), "New token is added in cache to call external endpoint and it expires in {ExpiresIn} with sliding expiration duration {Options}.", Convert.ToString(authTokenDetails.ExpiresIn, CultureInfo.InvariantCulture), JsonSerializer.Serialize(options));
        }

        /// <summary>
        /// Get authorization token from cache.
        /// </summary>
        /// <param name="key">Client Id.</param>
        /// <returns>Authorization token from cache</returns>
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