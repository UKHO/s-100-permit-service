namespace UKHO.S100PermitService.Common.Providers
{
    public interface IDigitalSignatureProvider
    {
        public byte[] GeneratePermitXmlHash(string permitXmlContent);
        public string SignHashWithPrivateKey(string privateKeySecret, byte[] hashContent);
    }
}
