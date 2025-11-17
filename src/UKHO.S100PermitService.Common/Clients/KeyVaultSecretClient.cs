using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using UKHO.S100PermitService.Common.Configuration;

namespace UKHO.S100PermitService.Common.Clients
{
    [ExcludeFromCodeCoverage]
    public class KeyVaultSecretClient : ISecretClient
    {
        private readonly SecretClient _secretClient;

        public KeyVaultSecretClient(IOptions<DataKeyVaultConfiguration> dataKeyVaultConfiguration, TokenCredential tokenCredential)
        {
            ArgumentNullException.ThrowIfNull(dataKeyVaultConfiguration);

            _secretClient = new SecretClient(new Uri(dataKeyVaultConfiguration.Value.ServiceUri), tokenCredential);
        }

        /// <summary>
        /// Get secret stored in key vault.
        /// </summary>
        /// <param name="secretName">Secret Key.</param>
        /// <returns>KeyVaultSecret details.</returns>
        public async Task<KeyVaultSecret> GetSecretAsync(string secretName)
        {
            return await _secretClient.GetSecretAsync(secretName);
        }

        /// <summary>
        /// Get resource containing all the properties of the secret except its value
        /// </summary>
        /// <returns>SecretProperties</returns>
        public AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync()
        {
            return _secretClient.GetPropertiesOfSecretsAsync();
        }
    }
}