using Microsoft.Extensions.Configuration;
using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IKeyVaultSecretService
    {
        void RefreshSecrets();      
        List<VaultSecret> FetchSecret(string keyName);
    }
}
