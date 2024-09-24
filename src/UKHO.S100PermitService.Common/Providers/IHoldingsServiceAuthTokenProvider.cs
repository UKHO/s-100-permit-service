namespace UKHO.S100PermitService.Common.Providers
{
    public interface IHoldingsServiceAuthTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
