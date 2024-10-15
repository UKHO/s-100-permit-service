using UKHO.S100PermitService.Common.Models.Holdings;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IHoldingsService
    {
        Task<List<HoldingsServiceResponse>> GetHoldingsAsync(int licenceId, CancellationToken cancellationToken, string correlationId);

        List<HoldingsServiceResponse> FilterHoldingsByLatestExpiry(List<HoldingsServiceResponse> holdingsServiceResponse);
    }
}
