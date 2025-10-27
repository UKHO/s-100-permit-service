using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Transformers;

namespace UKHO.S100PermitService.Common.Services
{
    public class PermitSignGeneratorService : IPermitSignGeneratorService
    {
        private readonly IDigitalSignatureProvider _digitalSignatureProvider;
        private readonly IKeyVaultService _keyVaultService;
        private readonly IOptions<DataKeyVaultConfiguration> _dataKeyVaultConfiguration;
        private readonly IXmlTransformer _xmlTransformer;

        public PermitSignGeneratorService(IDigitalSignatureProvider digitalSignatureProvider, IKeyVaultService keyVaultService, IOptions<DataKeyVaultConfiguration> dataKeyVaultConfiguration, IXmlTransformer xmlTransformer)
        {
            _digitalSignatureProvider = digitalSignatureProvider ?? throw new ArgumentNullException(nameof(digitalSignatureProvider));
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _dataKeyVaultConfiguration = dataKeyVaultConfiguration ?? throw new ArgumentNullException(nameof(dataKeyVaultConfiguration));
            _xmlTransformer = xmlTransformer ?? throw new ArgumentNullException(nameof(xmlTransformer));
        }

        /// <summary>Generates a base64-encoded digital signature for the given permit XML content using an ECDsa private key.</summary>
        /// <param name="permitXmlContent">The XML content of the permit to be signed.</param>
        /// <returns>
        /// A base64-encoded string representing the digital signature of the permit XML content.
        /// </returns>
        public async Task<string> GeneratePermitSignXmlAsync(string permitXmlContent)
        {
            //Generate the hash of the permit XML content.
            var permitXmlHash = _digitalSignatureProvider.GeneratePermitXmlHash(permitXmlContent);

            //Retrieved the data server's private key from the Key Vault.
            var privateKeySecret = await _keyVaultService.GetSecretKeys(_dataKeyVaultConfiguration.Value.DsPrivateKey);

            //Retrieved the data server's certificate from the Key Vault.
            var certificateSecret = _keyVaultService.GetCertificate(_dataKeyVaultConfiguration.Value.DsCertificate);

            var certificate = new X509Certificate2(certificateSecret);

            //Sign the hash using the private key in ECDsa format and hash of the permit XML content.
            var signatureBase64 = _digitalSignatureProvider.SignHashWithPrivateKey(privateKeySecret, permitXmlHash);

            // Create a StandaloneDigitalSignature object that encapsulates the certificate and the base64-encoded signature.
            var signature = _digitalSignatureProvider.CreateStandaloneDigitalSignature(certificate, signatureBase64);

            // Serialize the StandaloneDigitalSignature object to its XML representation.
            var signatureXml = await _xmlTransformer.SerializeToXml(signature);

            // Return the serialized XML string containing the digital signature.
            return signatureXml;
        }
    }
}