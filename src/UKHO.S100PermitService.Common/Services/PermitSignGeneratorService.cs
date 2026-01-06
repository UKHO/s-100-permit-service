using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
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
        private readonly ILogger<PermitSignGeneratorService> _logger;

        public PermitSignGeneratorService(
            IDigitalSignatureProvider digitalSignatureProvider,
            IKeyVaultService keyVaultService,
            IOptions<DataKeyVaultConfiguration> dataKeyVaultConfiguration,
            IXmlTransformer xmlTransformer,
            ILogger<PermitSignGeneratorService> logger)
        {
            _digitalSignatureProvider = digitalSignatureProvider ?? throw new ArgumentNullException(nameof(digitalSignatureProvider));
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _dataKeyVaultConfiguration = dataKeyVaultConfiguration ?? throw new ArgumentNullException(nameof(dataKeyVaultConfiguration));
            _xmlTransformer = xmlTransformer ?? throw new ArgumentNullException(nameof(xmlTransformer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Generates a base64-encoded digital signature for the given permit XML content using an ECDsa private key.</summary>
        /// <param name="permitXmlContent">The XML content of the permit to be signed.</param>
        /// <returns>
        /// A base64-encoded string representing the digital signature of the permit XML content.
        /// </returns>
        public async Task<string> GeneratePermitSignXmlAsync(string permitXmlContent)
        {
            _logger.LogInformation(EventIds.PermitSignCreationStarted.ToEventId(), "Permit sign generation started.");

            try
            {
                //Generate the hash of the permit XML content.
                _logger.LogInformation(EventIds.PermitHashGenerationStarted.ToEventId(), "Permit hash generation started.");
                var permitXmlHash = _digitalSignatureProvider.GeneratePermitXmlHash(permitXmlContent);
                _logger.LogInformation(EventIds.PermitHashGenerationCompleted.ToEventId(), "Permit hash successfully generated.");

                //Retrieved the data server's private key from the Key Vault.
                _logger.LogInformation("Retrieving data server private key from Key Vault.");
                var privateKeySecret = await _keyVaultService.GetSecretKeys(_dataKeyVaultConfiguration.Value.DsPrivateKey);

                //Retrieved the data server's certificate from the Key Vault.
                var useSecretBased = _dataKeyVaultConfiguration.Value.UseSecretStringForCert;
                var kvValue = useSecretBased
                    ? _dataKeyVaultConfiguration.Value.DsCertificateSecret
                    : _dataKeyVaultConfiguration.Value.DsCertificate;

                _logger.LogInformation("Retrieving data server certificate from Key Vault. Using {SourceType} source: {CertificateName}",
                    useSecretBased ? "Secrets" : "Certificates", kvValue);

                var certificateSecret = _keyVaultService.GetCertificate(kvValue);
                var certificate = new X509Certificate2(certificateSecret);
                _logger.LogInformation("Certificate loaded. Subject: {Subject}", certificate.Subject);

                //Sign the hash using the private key in ECDsa format and hash of the permit XML content.
                _logger.LogInformation("Signing permit hash with private key.");
                var signatureBase64 = _digitalSignatureProvider.SignHashWithPrivateKey(privateKeySecret, permitXmlHash);

                // Create a StandaloneDigitalSignature object that encapsulates the certificate and the base64-encoded signature.
                var signature = _digitalSignatureProvider.CreateStandaloneDigitalSignature(certificate, signatureBase64);

                // Serialize the StandaloneDigitalSignature object to its XML representation.
                _logger.LogInformation("Serializing digital signature to XML.");
                var signatureXml = await _xmlTransformer.SerializeToXml(signature);

                _logger.LogInformation(EventIds.PermitSignCreationCompleted.ToEventId(), "Permit sign creation completed.");

                // Return the serialized XML string containing the digital signature.
                return signatureXml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during permit sign generation.");
                throw;
            }
        }
    }
}