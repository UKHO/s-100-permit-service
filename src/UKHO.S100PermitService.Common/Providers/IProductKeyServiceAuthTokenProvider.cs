namespace UKHO.S100PermitService.Common.Providers
{
    public interface IProductKeyServiceAuthTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}