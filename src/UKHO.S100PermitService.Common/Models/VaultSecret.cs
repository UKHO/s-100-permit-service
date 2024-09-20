using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class VaultSecret
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
