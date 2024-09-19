namespace UKHO.S100PermitService.Common.Configuration
{
    public class HoldingsServiceApiConfiguration : IHoldingsServiceApiConfiguration
    {
        public string ClientId { get; set; }
        public string BaseUrl { get; set; }
    }
}
