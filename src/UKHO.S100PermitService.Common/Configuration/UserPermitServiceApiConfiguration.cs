namespace UKHO.S100PermitService.Common.Configuration
{
    public class UserPermitServiceApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public int RequestTimeoutInMinutes { get; set; }
    }
}
