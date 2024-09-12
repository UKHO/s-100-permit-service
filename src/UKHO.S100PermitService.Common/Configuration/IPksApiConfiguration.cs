namespace UKHO.S100PermitService.Common.Configuration
{
    public interface IPksApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
    }
}