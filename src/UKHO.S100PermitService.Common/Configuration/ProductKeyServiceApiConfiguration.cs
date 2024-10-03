using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class ProductKeyServiceApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public int RequestTimeoutInMinutes { get; set; }
        public string PermitHardwareId { get; set; }
    }
}