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
        public string GetCacheValue(string key)
        {
            _memoryCache.TryGetValue(key, out string? value);
            return value;
        }

        /// <summary>
        /// Set cache data in cache memory.
        /// </summary>
        /// <param name="key">Cache Key.</param>
        /// <param name="value">Cache Value.</param>
        public void SetCache(string key, string value)
        {
            _memoryCache.Set(key, value);
        }

        /// <summary>
        /// Stores a certificate in the cache memory with the specified key.
        /// </summary>
        /// <param name="key">The unique identifier for the cache entry.</param>
        /// <param name="value">The certificate data to be cached as a byte array.</param>
        public void SetCertificateCache(string key, byte[] value)
        {
            _memoryCache.Set(key, value);
        }

        /// <summary>
        /// Retrieves the certificate data associated with the specified key from the cache memory.
        /// </summary>
        /// <param name="key">The unique identifier for the cache entry.</param>
        /// <returns>The cached certificate data as a byte array, or null if the key does not exist.</returns>
        public byte[] GetCertificateCacheValue(string key)
        {
            return _memoryCache.TryGetValue(key, out byte[]? value) ? value : null;
        }
    }
}