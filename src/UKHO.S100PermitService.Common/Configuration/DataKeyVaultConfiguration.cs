using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class DataKeyVaultConfiguration
    {
        public string ServiceUri { get; set; }
        public string DsPrivateKey { get; set; }
        public string DsCertificate { get; set; }
    }
}