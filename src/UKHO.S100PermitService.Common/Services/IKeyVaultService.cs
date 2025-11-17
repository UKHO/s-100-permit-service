namespace UKHO.S100PermitService.Common.Services
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretKeys(string secretName);
        byte[] GetCertificate(string certificateName);
    }
}
