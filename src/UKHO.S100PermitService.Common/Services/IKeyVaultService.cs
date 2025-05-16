namespace UKHO.S100PermitService.Common.Services
{
    public interface IKeyVaultService
    {
        string GetSecretKeys(string secretName);
        byte[] GetCertificate(string certificateName);
    }
}
