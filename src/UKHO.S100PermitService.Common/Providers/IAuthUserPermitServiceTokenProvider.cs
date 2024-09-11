namespace UKHO.S100PermitService.Common.Providers
{
    public interface IAuthUserPermitServiceTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
