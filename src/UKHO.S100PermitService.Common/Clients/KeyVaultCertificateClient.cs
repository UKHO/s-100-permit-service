using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using UKHO.S100PermitService.Common.Configuration;

namespace UKHO.S100PermitService.Common.Clients
{
    [ExcludeFromCodeCoverage]
    public class KeyVaultCertificateClient : ICertificateClient
    {
        private readonly CertificateClient _certificateClient;

        public KeyVaultCertificateClient(IOptions<DataKeyVaultConfiguration> dataKeyVaultConfiguration, TokenCredential tokenCredential)
        {
            ArgumentNullException.ThrowIfNull(dataKeyVaultConfiguration);

            _certificateClient = new CertificateClient(new Uri(dataKeyVaultConfiguration.Value.ServiceUri), tokenCredential);
        }

        /// <summary>
        /// Get certificate stored in key vault.
        /// </summary>
        /// <param name="certificateName">Certificate name.</param>
        /// <returns>Certificate as a byte array.</returns>
        public byte[] GetCertificate(string certificateName)
        {
            var response = _certificateClient.GetCertificate(certificateName);
            return response.Value.Cer;
        }
    }
}