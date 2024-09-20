namespace UKHO.S100PermitService.Common.Clients
{
    public interface IProductkeyServiceApiClient
    {
        Task<HttpResponseMessage> CallProductkeyServiceApiAsync(string uri, HttpMethod httpMethod, string payload, string accessToken, string correlationId);
    }
}