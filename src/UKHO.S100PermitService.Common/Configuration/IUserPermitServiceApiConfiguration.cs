namespace UKHO.S100PermitService.Common.Configuration
{
    interface IUserPermitServiceApiConfiguration
    {
        string BaseUrl { get; set; }
        string ClientId { get; set; }
    }
}
