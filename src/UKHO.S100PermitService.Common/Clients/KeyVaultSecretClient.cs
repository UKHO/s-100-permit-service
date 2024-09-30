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
        private readonly IOptions<ManufacturerKeyConfiguration> _manufacturerKeyVault;

        public KeyVaultSecretClient(IOptions<ManufacturerKeyConfiguration> manufacturerKeyVault)
        {
            _manufacturerKeyVault = manufacturerKeyVault;
            _secretClient = new SecretClient(new Uri(_manufacturerKeyVault.Value.ServiceUri), new DefaultAzureCredential());
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
