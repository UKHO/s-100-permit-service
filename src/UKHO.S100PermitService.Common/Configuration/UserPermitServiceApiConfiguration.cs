namespace UKHO.S100PermitService.Common.Configuration
{
    public class UserPermitServiceApiConfiguration : IUserPermitServiceApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
    }
}
