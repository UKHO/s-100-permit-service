using Azure;
using Azure.Security.KeyVault.Secrets;

namespace UKHO.S100PermitService.Common.Clients
{
    public interface ISecretClient 
    {
        Task<KeyVaultSecret> GetSecretAsync(string secretName);
        AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync();
    }    
}
