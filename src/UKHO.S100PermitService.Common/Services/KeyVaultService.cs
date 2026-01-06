using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class KeyVaultService(ILogger<KeyVaultService> logger,
                                  ICacheProvider cacheProvider,
                                  ISecretClient secretClient,
                                  ICertificateClient certificateSecretClient,
                                  IOptions<DataKeyVaultConfiguration> dataKeyVaultConfiguration) : IKeyVaultService
    {
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
        public async Task<string> GetSecretKeys(string secretName)
        {
            try
            {
                var secretValue = cacheProvider.GetCacheValue(secretName);
                if(string.IsNullOrEmpty(secretValue))
                {
                    secretValue = (await GetSetSecretKeyValue(secretName)).Value;
                }
                else
                {
                    logger.LogInformation(EventIds.SecretKeyFoundInCache.ToEventId(), "Secret Key found in Cache.");
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
                var certValue = cacheProvider.GetCertificateCacheValue(certificateName);
                if(certValue.Length == 0)
                {
                    if(dataKeyVaultConfiguration.Value.UseSecretStringForCert)
                    { 
                        certValue = GetCertificateValueFromSecretAsync(certificateName).Result; 
                    }
                    else
                    {
                        certValue = GetSetCertificateValue(certificateName);
                    }
                }
                else
                {
                    logger.LogInformation(EventIds.CertificateFoundInCache.ToEventId(), "Certificate found in Cache.");
                }

                return certValue;
            }
            catch(Exception ex)
            {
                throw new PermitServiceException(EventIds.CertificateNameNotFoundInKeyVault.ToEventId(), "No Certificate found in Certificate Key Vault, failed with Exception :{Message}", ex.Message);
            }
        }

        /// <summary>
        /// Get Secret Key from keyVault and add into cache.
        /// </summary>
        /// <param name="secretName">SecretName.</param>
        /// <returns>SecretKeys</returns>
        private async Task<KeyVaultSecret> GetSetSecretKeyValue(string secretName)
        {
            var secretValue = await secretClient.GetSecretAsync(secretName);

            cacheProvider.SetCache(secretName, secretValue.Value);

            logger.LogInformation(EventIds.AddingNewSecretKeyInCache.ToEventId(), "New Secret Key added in Cache.");

            return secretValue;
        }

        /// <summary>  
        /// Retrieves the certificate associated with the requested certificate name from Key Vault and stores it in the cache.  
        /// </summary>  
        /// <param name="secretName">The name of the certificate to retrieve.</param>  
        /// <returns>The certificate as a byte array.</returns>
        private byte[] GetSetCertificateValue(string secretName)
        {
            var secretValue = certificateSecretClient.GetCertificate(secretName);

            cacheProvider.SetCertificateCache(secretName, secretValue.Cer);

            logger.LogInformation(EventIds.AddingNewCertificateInCache.ToEventId(), "New Certificate added in Cache.");

            return secretValue.Cer;
        }

        private async Task<byte[]> GetCertificateValueFromSecretAsync(string secretName)
        {
            var secretValue = await secretClient.GetSecretAsync(secretName);
            var value = ParseCertificateBytes(secretValue.Value);
            cacheProvider.SetCertificateCache(secretName, value);

            return value;
        }

        private static byte[] ParseCertificateBytes(string value)
        {
            // PEM format with certificate (may include private key)
            if(value.Contains("-----BEGIN CERTIFICATE-----"))
            {
                var certStart = value.IndexOf("-----BEGIN CERTIFICATE-----");
                var certEnd = value.IndexOf("-----END CERTIFICATE-----") + "-----END CERTIFICATE-----".Length;
                var certPem = value[certStart..certEnd];

                var cert = X509Certificate2.CreateFromPem(certPem);
                return cert.RawData;
            }

            // Try Base64 (DER or PFX encoded)
            if(IsBase64String(value))
            {
                return Convert.FromBase64String(value);
            }

            // Try Hex string
            if(IsHexString(value))
            {
                return Convert.FromHexString(value);
            }

            throw new FormatException($"Unrecognized certificate format in secret.");
        }

        private static bool IsBase64String(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Remove whitespace and check length
            var trimmed = value.Replace("\r", "").Replace("\n", "").Trim();
            if(trimmed.Length % 4 != 0)
            {
                return false;
            }
            try
            {
                Convert.FromBase64String(trimmed);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsHexString(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if(value.Length % 2 != 0)
            {
                return false;
            }

            return value.All(char.IsAsciiHexDigit);
        }
    }
}