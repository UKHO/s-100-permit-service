﻿using Microsoft.Extensions.Caching.Memory;
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

        public string GetCacheKey(string key)
        {
            _memoryCache.TryGetValue(key, out string? Value);
            return Value;
        }

        public void SetCacheKey(string key, string value)
        {            
            _memoryCache.Set(key, value);
        }
    }
}
