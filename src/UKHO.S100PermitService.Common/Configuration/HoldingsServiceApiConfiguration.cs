namespace UKHO.S100PermitService.Common.Configuration
{
    public class HoldingsServiceApiConfiguration
    {
        public string ClientId { get; set; }
        public string BaseUrl { get; set; }
        public int RequestTimeoutInMinutes { get; set; }
    }
}
