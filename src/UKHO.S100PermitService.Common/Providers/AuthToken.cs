using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Providers
{
    [ExcludeFromCodeCoverage]
    public class AuthToken
    {
        public string? AccessToken { get; set; }
        public DateTime ExpiresIn { get; set; }
    }
}