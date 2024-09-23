namespace UKHO.S100PermitService.Common.Clients
{
    public interface IProductKeyServiceApiClient
    {
        Task<HttpResponseMessage> CallProductKeyServiceApiAsync(string uri, HttpMethod httpMethod, string payLoad, string accessToken, string correlationId);
    }
}
