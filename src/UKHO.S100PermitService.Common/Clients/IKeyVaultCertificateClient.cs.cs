using Azure.Security.KeyVault.Certificates;

namespace UKHO.S100PermitService.Common.Clients
{
    public interface IKeyVaultCertificateClient
    {
        KeyVaultCertificate GetCertificate(string certificateName);
    }
}