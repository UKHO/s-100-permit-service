using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Cache
{
    [ExcludeFromCodeCoverage]
    public class CachingEngine
    {        
        public static MemoryCache CreateMemoryCache()
        {
            var memoryCacheOptions = new MemoryCacheOptions();
            return new MemoryCache(memoryCacheOptions);
        }
    }
}

