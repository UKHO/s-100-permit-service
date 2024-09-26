using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

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
            _memoryCache = CreateMemoryCache();
            _keyVaultEndpoint = configuration["ManufacturerKeyVault:ServiceUri"]!;
            _secretClient = new SecretClient(new Uri(_keyVaultEndpoint), new DefaultAzureCredential());
            ///CacheManufactureKeysAsync();
            new Action(async () => await CacheManufacturerKeysAsync())();
        }

        //application start
        public async Task CacheManufacturerKeysAsync()
        {
            if(!string.IsNullOrEmpty(_keyVaultEndpoint))
            {
                var secretProperties = _secretClient.GetPropertiesOfSecrets();
                foreach(var secretProperty in secretProperties)
                {
                    var secretName = secretProperty.Name;
                    var secretValues  = await GetSetManufacturerValue(secretName);
                }
                //log exception- no secrets found
            }
        }

        // call from controller
        public async Task<string> GetManufacturerKeysAsync(string secretName)
        {
            if(_memoryCache.TryGetValue(secretName, out string? secretValue))
            {
                return secretValue;
            }

            var secret = await GetSetManufacturerValue(secretName);
            return secret;
        }

        //child method
        private async Task<string> GetSetManufacturerValue(string secretName)
        {
            var secret = (await _secretClient.GetSecretAsync(secretName)).Value.Value;

            _memoryCache.Set(secretName, secret, _defaultCacheExpiryDuration);
            return secret;
        }

        private static MemoryCache CreateMemoryCache()
        {
            var memoryCacheOptions = new MemoryCacheOptions();
            return new MemoryCache(memoryCacheOptions);
        }
    }
}
