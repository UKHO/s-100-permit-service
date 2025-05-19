using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly ILogger<KeyVaultService> _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISecretClient _secretClient;
        private readonly ICertificateClient _certificateSecretClient;

        public KeyVaultService(ILogger<KeyVaultService> logger,
                                      ICacheProvider cacheProvider,
                                      ISecretClient secretClient,
                                      ICertificateClient certificateSecretClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
            _certificateSecretClient = certificateSecretClient ?? throw new ArgumentNullException(nameof(certificateSecretClient)); ;
        }

        /// <summary>
        /// Get the secret keys associated with the requested data.
        /// </summary>
        /// <remarks>
        /// If SecretKey does not exists in cache then get from Key Vault and add in cache.
        /// If SecretKey does not exists in Key Vault then PermitServiceException exception will be thrown.
        /// </remarks> 
        /// <param name="secretName">secret Name.</param>
        /// <returns>Secret Key.</returns>
        /// <exception cref="PermitServiceException">PermitServiceException exception will be thrown when exception occurred.</exception>
        public string GetSecretKeys(string secretName)
        {
            try
            {
                var secretValue = _cacheProvider.GetCacheValue(secretName);
                if(string.IsNullOrEmpty(secretValue))
                {
                    secretValue = GetSetSecretKeyValue(secretName).Value;
                }
                else
                {
                    _logger.LogInformation(EventIds.SecretKeyFoundInCache.ToEventId(), "Secret Key found in Cache.");
                }

                return secretValue;
            }
            catch(Exception ex)
            {
                throw new PermitServiceException(EventIds.SecretNameNotFoundInKeyVault.ToEventId(), "No Secrets found in Secret Key Vault, failed with Exception :{Message}", ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the certificate associated with the requested certificate name.
        /// </summary>
        /// <remarks>
        /// If the certificate does not exist in the cache, it is retrieved from Key Vault and added to the cache.
        /// If the certificate does not exist in Key Vault, a PermitServiceException is thrown.
        /// </remarks>
        /// <param name="certificateName">The name of the certificate to retrieve.</param>
        /// <returns>The certificate as a byte array.</returns>
        /// <exception cref="PermitServiceException">Thrown when the certificate cannot be found in Key Vault.</exception>
        public byte[] GetCertificate(string certificateName)
        {
            try
            {
                var certValue = _cacheProvider.GetCertificateCacheValue(certificateName);
                if(certValue == null)
                {
                    certValue = GetSetCertificateValue(certificateName);
                }
                else
                {
                    _logger.LogInformation(EventIds.SecretKeyFoundInCache.ToEventId(), "Certificate found in Cache.");
                }

                return certValue;
            }
            catch(Exception ex)
            {
                throw new PermitServiceException(EventIds.SecretNameNotFoundInKeyVault.ToEventId(), "No Certificate found in Certificate Key Vault, failed with Exception :{Message}", ex.Message);
            }
        }

        /// <summary>
        /// Get Secret Key from keyVault and add into cache.
        /// </summary>
        /// <param name="secretName">SecretName.</param>
        /// <returns>SecretKeys</returns>
        private KeyVaultSecret GetSetSecretKeyValue(string secretName)
        {
            var secretValue = _secretClient.GetSecret(secretName);

            _cacheProvider.SetCache(secretName, secretValue.Value);

            _logger.LogInformation(EventIds.AddingNewSecretKeyInCache.ToEventId(), "New Secret Key added in Cache.");

            return secretValue;
        }

        /// <summary>  
        /// Retrieves the certificate associated with the requested certificate name from Key Vault and stores it in the cache.  
        /// </summary>  
        /// <param name="secretName">The name of the certificate to retrieve.</param>  
        /// <returns>The certificate as a byte array.</returns>
        private byte[] GetSetCertificateValue(string secretName)
        {
            var secretValue = _certificateSecretClient.GetCertificate(secretName);

            _cacheProvider.SetCertificateCache(secretName, secretValue.Cer);

            _logger.LogInformation(EventIds.AddingNewSecretKeyInCache.ToEventId(), "New Certificate added in Cache.");

            return secretValue.Cer;
        }
    }
}