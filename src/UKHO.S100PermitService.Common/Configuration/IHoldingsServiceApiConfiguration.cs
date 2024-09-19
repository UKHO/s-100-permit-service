namespace UKHO.S100PermitService.Common.Configuration
{
    public interface IHoldingsServiceApiConfiguration
    {
        public string ClientId { get; set; }
        public string BaseUrl { get; set; }
    }
}
