namespace UKHO.S100PermitService.Common.Helpers
{
    public interface IProductkeyServiceApiClient
    {
        Task<HttpResponseMessage> CallProductkeyServiceApiAsync(string uri, HttpMethod httpMethod, string payload, string accessToken, string correlationId);
    }
}