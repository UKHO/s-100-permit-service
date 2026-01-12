namespace UKHO.S100PermitService.Common.Clients
{
    public interface ICertificateClient
    {
        byte[] GetCertificate(string certificateName);
    }
}