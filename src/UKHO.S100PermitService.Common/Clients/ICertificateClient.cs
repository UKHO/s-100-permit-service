using Azure.Security.KeyVault.Certificates;

namespace UKHO.S100PermitService.Common.Clients
{
    public interface ICertificateClient
    {
        KeyVaultCertificate GetCertificate(string certificateName);
    }
}