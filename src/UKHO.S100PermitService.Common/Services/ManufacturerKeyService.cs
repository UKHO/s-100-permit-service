using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using UKHO.S100PermitService.Common.Cache;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.Services
{
    public class ManufacturerKeyService : IManufacturerKeyService
    {
        private readonly TimeSpan _defaultCacheExpiryDuration = TimeSpan.FromHours(value: 2);
        private readonly IMemoryCache _memoryCache;
        private readonly SecretClient _secretClient;
        private readonly string _keyVaultEndpoint;

        public ManufacturerKeyService(IConfiguration configuration)
        {
            _keyVaultEndpoint = configuration["ManufacturerKeyVault:ServiceUri"]!;
            _secretClient = new SecretClient(new Uri(_keyVaultEndpoint), new DefaultAzureCredential());
            _memoryCache = CachingEngine.CreateMemoryCache();
            new Action(async () => await CacheManufacturerKeysAsync())();
        }

        //application start
        public async Task CacheManufacturerKeysAsync()
        {
            if(!string.IsNullOrEmpty(_keyVaultEndpoint))
            {
                var secretProperties = _secretClient.GetPropertiesOfSecrets();

                if(!secretProperties.Any())
                {
                    throw new PermitServiceException(EventIds.ManufacturerIdNotFoundInCache.ToEventId(), "No Secrets found in Manufacturer Keyvault");
                }
                else
                {
                    foreach(var secretProperty in secretProperties)
                    {
                        var secretName = secretProperty.Name;
                        await GetSetManufacturerValue(secretName);
                    }
                }
            }
        }

        // call from controller
        public async Task<string> GetManufacturerKeysAsync(string secretName)
        {
            try
            {
                if(_memoryCache.TryGetValue(secretName, out string? secretValue))
                {
                    return secretValue;
                }

                var secret = await GetSetManufacturerValue(secretName);
                return secret;
            }
            catch
            {
                throw new PermitServiceException(EventIds.ManufacturerIdNotFoundInCache.ToEventId(), "No Secret found for M_Id in Manufacturer Keyvault");
            }
        }

        //child method
        private async Task<string> GetSetManufacturerValue(string secretName)
        {
            var secret = (await _secretClient.GetSecretAsync(secretName)).Value.Value;

            _memoryCache.Set(secretName, secret, _defaultCacheExpiryDuration);
            return secret;
        }        
    }
}
