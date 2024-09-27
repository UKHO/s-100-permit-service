using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Clients
{
    [ExcludeFromCodeCoverage]
    public class KeyVaultSecretClient : ISecretClient
    {
        private readonly SecretClient _secretClient;

        public KeyVaultSecretClient(Uri keyVaultUri)
        {
            _secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
        }

        public KeyVaultSecret GetSecret(string secretName)
        {
            return _secretClient.GetSecret(secretName);
        }

        public IEnumerable<SecretProperties> GetPropertiesOfSecrets()
        {
            return _secretClient.GetPropertiesOfSecrets();
        }
    }
}
