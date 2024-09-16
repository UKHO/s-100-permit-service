namespace UKHO.S100PermitService.API.FunctionalTests.Configuration
{
    public class TokenConfiguration
    {
        public string? ClientId { get; set; }
        public string? ClientIdNoAuth { get; set; }
        public string? TenantId { get; set; }
        public string? MicrosoftOnlineLoginUrl { get; set; }
        public string? ClientSecret { get; set; }
        public string? ClientSecretNoAuth { get; set; }
        public bool IsRunningOnLocalMachine { get; set; }
    }
}