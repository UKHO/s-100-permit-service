namespace UKHO.S100PermitService.Common.Helpers
{
    public interface IHoldingsApiClient
    {
        Task<HttpResponseMessage> GetHoldingsAsync(string uri, int licenceId, string accessToken, string correlationId);
    }
}
