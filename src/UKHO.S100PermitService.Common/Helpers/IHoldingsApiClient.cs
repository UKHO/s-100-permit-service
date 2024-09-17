namespace UKHO.S100PermitService.Common.Helpers
{
    public interface IHoldingsApiClient
    {
        Task<HttpResponseMessage> GetHoldingsDataAsync(string uri, int licenceId, string accessToken, string correlationId);
    }
}
