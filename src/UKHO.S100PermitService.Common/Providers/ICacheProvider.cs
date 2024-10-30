namespace UKHO.S100PermitService.Common.Providers
{
    public interface ICacheProvider
    {
        public string GetCacheValue(string key);
        public void SetCache(string key, string value);
    }
}
