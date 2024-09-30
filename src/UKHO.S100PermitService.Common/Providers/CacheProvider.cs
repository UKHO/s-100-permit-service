using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Providers
{
    [ExcludeFromCodeCoverage]
    public class CacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _memoryCache;

        public CacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public string GetCacheKey(string key)
        {
            _memoryCache.TryGetValue(key, out string? Value);
            return Value;
        }

        public void SetCacheKey(string key, string value, TimeSpan timeSpan)
        {            
            _memoryCache.Set(key, value, timeSpan);
        } 
    }
}
