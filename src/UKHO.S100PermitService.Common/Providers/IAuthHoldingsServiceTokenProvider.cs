namespace UKHO.S100PermitService.Common.Providers
{
    public interface IAuthHoldingsServiceTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
