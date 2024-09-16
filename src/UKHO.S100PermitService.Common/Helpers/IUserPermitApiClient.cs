namespace UKHO.S100PermitService.Common.Helpers
{
    public interface IUserPermitApiClient
    {
        Task<HttpResponseMessage> GetUserPermitsAsync(string uri, int licenceId, string accessToken, string correlationId = null);
    }
}
