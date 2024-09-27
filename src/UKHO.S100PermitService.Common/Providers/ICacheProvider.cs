namespace UKHO.S100PermitService.Common.Providers
{
    public interface ICacheProvider
    {
        public string GetCacheKey(string key);
        public string SetCacheKey(string key, string value, TimeSpan timeSpan);
    }
}
