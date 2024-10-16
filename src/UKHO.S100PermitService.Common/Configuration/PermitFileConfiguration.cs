using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class PermitFileConfiguration
    {
        public string DataServerName { get; set; }
        public string DataServerIdentifier { get; set; }
    }
}
