using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class AuthTokenDetails
    {
        public string? AccessToken { get; set; }
        public DateTime ExpiresIn { get; set; }
    }
}
