using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class PermitServiceManagedIdentityConfiguration
    {
        public double DeductTokenExpiryMinutes { get; set; }
    }
}
