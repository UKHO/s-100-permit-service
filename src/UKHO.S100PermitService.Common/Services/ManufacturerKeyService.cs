using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class ManufacturerKeyService : IManufacturerKeyService
    {
        private readonly IOptions<ManufacturerKeyConfiguration> _manufacturerKeyvault;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISecretClient _secretClient;

        public ManufacturerKeyService(IOptions<ManufacturerKeyConfiguration> manufacturerKeyvault,
                                        ICacheProvider cacheProvider,
                                        ISecretClient secretClient)
        {
            _manufacturerKeyvault = manufacturerKeyvault ?? throw new ArgumentNullException(nameof(manufacturerKeyvault));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
            CacheManufacturerKeys();
        }

        public void CacheManufacturerKeys()
        {
            if(!string.IsNullOrEmpty(_manufacturerKeyvault.Value.ServiceUri))
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
                        GetSetManufacturerValue(secretName);
                    }
                }
            }
        }

        public string GetManufacturerKeys(string secretName)
        {
            try
            {
                var secretValue = _cacheProvider.GetCacheKey(secretName);
                if(string.IsNullOrEmpty(secretValue))
                {
                    var secret = GetSetManufacturerValue(secretName);
                    return secret.Value.ToString();
                }
                return secretValue;
            }
            catch
            {
                throw new PermitServiceException(EventIds.ManufacturerIdNotFoundInCache.ToEventId(), "No Secret found for M_Id in Manufacturer Keyvault");
            }
        }

        private KeyVaultSecret GetSetManufacturerValue(string secretName)
        {
            var secretValue = _secretClient.GetSecret(secretName);

            _cacheProvider.SetCacheKey(secretName, secretValue.Value, CacheExpiryDuration());
            return secretValue;
        }

        private TimeSpan CacheExpiryDuration()
        {
            return TimeSpan.FromHours(value: _manufacturerKeyvault.Value.CacheTimeoutInHours);
        }
    }
}