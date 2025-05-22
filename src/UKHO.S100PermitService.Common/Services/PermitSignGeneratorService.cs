using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Services
{
    public class PermitSignGeneratorService : IPermitSignGeneratorService
    {
        private readonly IDigitalSignatureProvider _digitalSignatureProvider;
        private readonly IKeyVaultService _keyVaultService;
        private readonly IOptions<DataKeyVaultConfiguration> _dataKeyVaultConfiguration;

        public PermitSignGeneratorService(IDigitalSignatureProvider digitalSignatureProvider, IKeyVaultService keyVaultService, IOptions<DataKeyVaultConfiguration> dataKeyVaultConfiguration)
        {
            _digitalSignatureProvider = digitalSignatureProvider ?? throw new ArgumentNullException(nameof(digitalSignatureProvider));
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _dataKeyVaultConfiguration = dataKeyVaultConfiguration ?? throw new ArgumentNullException(nameof(dataKeyVaultConfiguration));
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
            var privateKeySecret = _keyVaultService.GetSecretKeys(_dataKeyVaultConfiguration.Value.DsPrivateKey);

            //Sign the hash using the private key in ECDsa format and hash of the permit XML content.
            var signatureBase64 = _digitalSignatureProvider.SignHashWithPrivateKey(privateKeySecret, permitXmlHash);

            return string.Empty;
        }
    }
}