using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class AzureAdConfiguration
    {
        public string MicrosoftOnlineLoginUrl { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
    }
}