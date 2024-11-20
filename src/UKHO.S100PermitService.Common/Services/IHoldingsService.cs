using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.Holdings;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IHoldingsService
    {
        Task<ServiceResponseResult<List<HoldingsServiceResponse>>> GetHoldingsAsync(int licenceId, string correlationId, CancellationToken cancellationToken);

        public IEnumerable<HoldingsServiceResponse> FilterHoldingsByLatestExpiry(IEnumerable<HoldingsServiceResponse> holdingsServiceResponse);
    }
}