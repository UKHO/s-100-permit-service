using Azure.Security.KeyVault.Secrets;

namespace UKHO.S100PermitService.Common.Clients
{
    public interface ISecretClient 
    {
        KeyVaultSecret GetSecret(string secretName);
        IEnumerable<SecretProperties> GetPropertiesOfSecrets();
    }    
}
