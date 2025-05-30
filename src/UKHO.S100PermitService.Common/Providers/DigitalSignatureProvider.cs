using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Extensions;
using UKHO.S100PermitService.Common.Models.PermitSign;

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
        /// Signs the specified hash using a Base64-encoded ECDsa private key.
        /// </summary>
        /// <param name="privateKeySecret">The Base64-encoded string containing the ECDsa private key.</param>
        /// <param name="hashContent">The hash content to sign.</param>
        /// <returns>A base64-encoded string representing the digital signature.</returns>
        /// <exception cref="PermitServiceException">
        /// Thrown when the private key import or signing fails.
        /// </exception>
        public string SignHashWithPrivateKey(string privateKeySecret, byte[] hashContent)
        {
            try
            {
                using(var ecdsaPrivateKey = ECDsa.Create())
                {
                    ecdsaPrivateKey.ImportECPrivateKey(Convert.FromBase64String(privateKeySecret), out _);
                    var ecdsaSignature = ecdsaPrivateKey.SignHash(hashContent);

                    return ConvertToDerFormat(ecdsaSignature);
                }
            }
            catch(Exception ex)
            {
                throw new PermitServiceException(EventIds.PermitPrivateKeySigningFailed.ToEventId(), "An error occurred while signing the hash with the private key with exception: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Creates a StandaloneDigitalSignature object containing the certificate and digital signature details.
        /// </summary>
        /// <param name="certificate">The X509 certificate used for signing, containing issuer and subject information.</param>
        /// <param name="signatureBase64">The base64-encoded digital signature value.</param>
        /// <returns>
        /// A StandaloneDigitalSignature object populated with the filename, certificate details, and digital signature.
        /// </returns>
        public StandaloneDigitalSignature CreateStandaloneDigitalSignature(X509Certificate2 certificate, string signatureBase64)
        {
            _logger.LogInformation(EventIds.StandaloneDigitalSignatureGenerationStarted.ToEventId(), "StandaloneDigitalSignature generation process started.");

            var issuer = certificate.Issuer.GetCnFromCertificate();
            var certificateValue = Convert.ToBase64String(certificate.RawData);
            var certificateDsId = certificate.Subject.GetCnFromCertificate();

            var standaloneSignature = new StandaloneDigitalSignature
            {
                Filename = PermitServiceConstants.PermitXmlFileName,
                Certificate = new Certificate
                {
                    SchemeAdministrator = new SchemeAdministrator
                    {
                        Id = issuer
                    },
                    CertificateMetadata = new CertificateMetadata
                    {
                        Id = certificateDsId,
                        Issuer = issuer,
                        Value = certificateValue
                    }
                },
                DigitalSignatureInfo = new DigitalSignatureInfo
                {
                    Id = PermitServiceConstants.DigitalSignatureId,
                    CertificateRef = certificateDsId,
                    Value = signatureBase64
                }
            };

            _logger.LogInformation(EventIds.StandaloneDigitalSignatureGenerationCompleted.ToEventId(), "StandaloneDigitalSignature generation process completed.");

            return standaloneSignature;
        }

        /// <summary>
        /// Converts a raw ECDSA signature (r || s) into DER format, which is required for 
        /// compatibility with certain cryptographic standards. The method splits the signature 
        /// into its r and s components, encodes them as ASN.1 INTEGERs, and combines them 
        /// into an ASN.1 SEQUENCE.
        /// </summary>
        /// <param name="ecdsaSignature">The raw ECDSA signature as a byte array (concatenated r and s values).</param>
        /// <returns>A Base64-encoded string representing the DER-encoded ECDSA signature.</returns>
        private string ConvertToDerFormat(byte[] ecdsaSignature)
        {
            var halfLength = ecdsaSignature.Length / 2;
            var r = ecdsaSignature[..halfLength];
            var s = ecdsaSignature[halfLength..];

            var derR = EncodeAsn1Integer(r);
            var derS = EncodeAsn1Integer(s);

            using(var stream = new MemoryStream())
            {
                stream.WriteByte(0x30);
                stream.WriteByte((byte)(derR.Length + derS.Length));
                stream.Write(derR, 0, derR.Length);
                stream.Write(derS, 0, derS.Length);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        /// <summary>
        /// Encodes a byte array as an ASN.1 INTEGER.
        /// </summary>
        /// <param name="integerValue"></param>
        /// <returns>The ASN.1-encoded INTEGER as a byte array.</returns>
        private byte[] EncodeAsn1Integer(byte[] integerValue)
        {
            using(var memoryStream = new MemoryStream())
            {
                memoryStream.WriteByte(0x02);

                if(integerValue[0] >= 0x80)
                {
                    memoryStream.WriteByte((byte)(integerValue.Length + 1));
                    memoryStream.WriteByte(0x00);
                }
                else
                {
                    memoryStream.WriteByte((byte)integerValue.Length);
                }

                memoryStream.Write(integerValue, 0, integerValue.Length);
                return memoryStream.ToArray();
            }
        }
    }
}
