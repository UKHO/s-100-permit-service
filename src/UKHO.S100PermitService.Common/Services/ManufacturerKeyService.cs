using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class ManufacturerKeyService : IManufacturerKeyService
    {
        private readonly ILogger<ManufacturerKeyService> _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISecretClient _secretClient;

        public ManufacturerKeyService(ILogger<ManufacturerKeyService> logger,
                                      ICacheProvider cacheProvider,
                                      ISecretClient secretClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        }

        /// <summary>
        /// Get ManufacturerKeys (MKey) associated with the requested ManufacturerId (MId).
        /// </summary>
        /// <remarks>
        /// ManufacturerKey is used to decrypt HardwareId(HW_ID) from EncryptedHardwareId stored in UserPermit.
        /// If ManufacturerKey does not exists in cache then get from Key Vault and add in cache.
        /// If ManufacturerKey does not exists in Key Vault then trigger PermitServiceException exception handler.
        /// </remarks> 
        /// <param name="secretName">ManufacturerId (MId).</param>
        /// <returns>ManufacturerKey.</returns>
        /// <exception cref="PermitServiceException">PermitServiceException exception handler triggered when exception occurred.</exception>
        public string GetManufacturerKeys(string secretName)
        {
            try
            {
                var secretValue = _cacheProvider.GetCacheKey(secretName);
                if(string.IsNullOrEmpty(secretValue))
                {
                    secretValue = GetSetManufacturerValue(secretName).Value;
                }
                else
                {
                    _logger.LogInformation(EventIds.ManufacturerKeyFoundInCache.ToEventId(), "Manufacturer Key found in Cache | {DateTime}", DateTime.Now.ToUniversalTime());
                }
                return secretValue;
            }
            catch(Exception ex)
            {
                throw new PermitServiceException(EventIds.ManufacturerIdNotFoundInKeyVault.ToEventId(), "No Secrets found in Manufacturer Key Vault, failed with Exception :{Message}", ex.Message);
            }
        }

        /// <summary>
        /// Get ManufacturerKeys (MKey) from keyVault and add into cache.
        /// </summary>
        /// <param name="secretName">ManufacturerId(MId).</param>
        /// <returns>ManufacturerKeys</returns>
        private KeyVaultSecret GetSetManufacturerValue(string secretName)
        {
            var secretValue = _secretClient.GetSecret(secretName);

            _cacheProvider.SetCacheKey(secretName, secretValue.Value);

            _logger.LogInformation(EventIds.AddingNewManufacturerKeyInCache.ToEventId(), "New Manufacturer Key added in Cache | {DateTime}", DateTime.Now.ToUniversalTime());

            return secretValue;
        }
    }
}