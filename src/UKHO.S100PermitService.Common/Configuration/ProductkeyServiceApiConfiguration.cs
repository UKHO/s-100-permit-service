using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class ProductkeyServiceApiConfiguration : IProductkeyServiceApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
    }
}