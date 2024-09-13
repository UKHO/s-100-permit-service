namespace UKHO.S100PermitService.Common.Configuration
{
    public interface IProductkeyServiceApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
    }
}