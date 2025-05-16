namespace UKHO.S100PermitService.Common.Services
{
    public interface IDataKeyService
    {
        string GetSecretKeys(string secretName);
        byte[] GetCertificate(string certificateName);
    }
}
