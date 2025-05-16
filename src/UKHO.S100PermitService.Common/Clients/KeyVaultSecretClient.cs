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
        private readonly IOptions<DataKeyVaultConfiguration> _dataKeyVaultConfiguration;

        public KeyVaultSecretClient(IOptions<DataKeyVaultConfiguration> dataKeyVaultConfiguration)
        {
            _dataKeyVaultConfiguration = dataKeyVaultConfiguration ?? throw new ArgumentNullException(nameof(dataKeyVaultConfiguration));
            _secretClient = new SecretClient(new Uri(_dataKeyVaultConfiguration.Value.ServiceUri), new DefaultAzureCredential());
        }

        /// <summary>
        /// Get secret stored in key vault.
        /// </summary>
        /// <param name="secretName">Secret Key.</param>
        /// <returns>KeyVaultSecret details.</returns>
        public KeyVaultSecret GetSecret(string secretName)
        {
            return _secretClient.GetSecret(secretName);
        }

        /// <summary>
        /// Get resource containing all the properties of the secret except its value
        /// </summary>
        /// <returns>SecretProperties</returns>
        public IEnumerable<SecretProperties> GetPropertiesOfSecrets()
        {
            return _secretClient.GetPropertiesOfSecrets();
        }
    }
}