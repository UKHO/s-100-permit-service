using System.Security.Cryptography;

namespace UKHO.S100PermitService.Common.Providers
{
    public interface IDigitalSignatureProvider
    {
        public byte[] GeneratePermitXmlHash(string permitXmlContent);
        public ECDsa ImportEcdsaPrivateKey(string privateKeySecret);
        public string SignHash(ECDsa privateKey, byte[] hashContent);
    }
}
