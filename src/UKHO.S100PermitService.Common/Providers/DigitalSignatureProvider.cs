using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.Providers
{
    public class DigitalSignatureProvider : IDigitalSignatureProvider
    {
        private readonly ILogger<DigitalSignatureProvider> _logger;

        public DigitalSignatureProvider(ILogger<DigitalSignatureProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates a SHA-384 hash for the provided permit XML content.
        /// </summary>
        /// <param name="permitXmlContent">The XML content of the permit.</param>
        /// <returns>A byte array representing the hash of the permit XML content.</returns>
        /// <exception cref="PermitServiceException">Thrown when hash generation fails.</exception>
        public byte[] GeneratePermitXmlHash(string permitXmlContent)
        {
            try
            {
                _logger.LogInformation(EventIds.PermitHashGenerationStarted.ToEventId(), "Permit hash generation started.");

                using(var sha384 = SHA384.Create())
                {
                    var permitXmlHash = sha384.ComputeHash(Encoding.UTF8.GetBytes(permitXmlContent));
                    _logger.LogInformation(EventIds.PermitHashGenerationCompleted.ToEventId(), "Permit hash successfully generated.");
                    return permitXmlHash;
                }
            }
            catch(Exception ex)
            {
                throw new PermitServiceException(EventIds.PermitHashGenerationFailed.ToEventId(), "Permit hash generation failed with Exception: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Imports an ECDsa private key from a Base64-encoded string.
        /// </summary>
        /// <param name="privateKeySecret">The Base64-encoded string containing the ECDsa private key.</param>
        /// <returns>An <see cref="ECDsa"/> instance initialized with the provided private key.</returns>
        /// <exception cref="PermitServiceException">
        /// Thrown when the private key import fails due to invalid input or other errors.
        /// </exception>
        public ECDsa ImportEcdsaPrivateKey(string privateKeySecret)
        {
            try
            {
                var privateKeyData = Convert.FromBase64String(privateKeySecret);
                var ecdsaPrivateKey = ECDsa.Create();
                ecdsaPrivateKey.ImportECPrivateKey(privateKeyData, out _);
                return ecdsaPrivateKey;
            }
            catch(Exception ex)
            {
                throw new PermitServiceException(EventIds.PermitPrivateKeyImportFailed.ToEventId(), "Permit private key import failed with Exception: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Signs the specified hash using the provided ECDsa private key.
        /// </summary>
        /// <param name="privateKey">The <see cref="ECDsa"/> private key used for signing.</param>
        /// <param name="hashContent">The hash content to sign.</param>
        /// <returns>A base64-encoded string representing the digital signature.</returns>
        public string SignHash(ECDsa privateKey, byte[] hashContent)
        {
            return Convert.ToBase64String(privateKey.SignHash(hashContent));
        }
    }
}
