namespace UKHO.S100PermitService.Common.Providers
{
    public interface IAuthProductKeyServiceTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
