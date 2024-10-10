namespace UKHO.S100PermitService.Common.Providers
{
    public interface ICacheProvider
    {
        public string GetCacheKey(string key);
        public void SetCacheKey(string key, string value);
    }
}
