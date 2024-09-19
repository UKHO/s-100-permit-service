namespace UKHO.S100PermitService.Common.Providers
{
    public interface IUserPermitServiceAuthTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
