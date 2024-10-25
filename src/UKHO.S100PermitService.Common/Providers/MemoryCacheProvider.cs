using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Providers
{
    [ExcludeFromCodeCoverage]
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <summary>
        /// Get the value associated with the requested key from cache memory.
        /// </summary>
        /// <param name="key">Cache Key.</param>
        /// <returns>Cache value</returns>
        public string GetCacheKey(string key)
        {
            _memoryCache.TryGetValue(key, out string? Value);
            return Value;
        }

        /// <summary>
        /// Set cache data in cache memory.
        /// </summary>
        /// <param name="key">Cache Key.</param>
        /// <param name="value">Cache Value.</param>
        public void SetCacheKey(string key, string value)
        {            
            _memoryCache.Set(key, value);
        }
    }
}