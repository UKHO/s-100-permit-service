namespace UKHO.S100PermitService.Common.Clients
{
    public interface IUserPermitApiClient
    {
        Task<HttpResponseMessage> GetUserPermitsAsync(string uri, int licenceId, string accessToken, CancellationToken cancellationToken, string correlationId);
    }
}
