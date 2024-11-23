namespace UKHO.S100PermitService.Common.Configuration
{
    public class ProductKeyServiceApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public int RequestTimeoutInMinutes { get; set; }
        public string HardwareId { get; set; }
    }
}