namespace UKHO.S100PermitService.Common.Clients
{
    public interface IHoldingsApiClient
    {
        Task<HttpResponseMessage> GetHoldingsAsync(string uri, int licenceId, string accessToken, CancellationToken cancellationToken, string correlationId);
    }
}
