using System.Security.Cryptography.X509Certificates;
using UKHO.S100PermitService.Common.Models.PermitSign;

namespace UKHO.S100PermitService.Common.Providers
{
    public interface IDigitalSignatureProvider
    {
        public byte[] GeneratePermitXmlHash(string permitXmlContent);
        public string SignHashWithPrivateKey(string privateKeySecret, byte[] hashContent);
        public StandaloneDigitalSignature CreateStandaloneDigitalSignature(X509Certificate2 certificate, string signatureBase64);
    }
}
