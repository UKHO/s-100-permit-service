using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class ManufacturerKeyConfiguration
    {
        public string ServiceUri { get; set; }
        public int CacheTimeoutInHours { get; set; }
    }
}