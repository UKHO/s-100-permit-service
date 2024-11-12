using System.Net;
using UKHO.S100PermitService.Common.Models.Holdings;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IHoldingsService
    {
        Task<(HttpResponseMessage httpResponseMessage, IEnumerable<HoldingsServiceResponse>? holdingsServiceResponse)> GetHoldingsAsync(int licenceId, CancellationToken cancellationToken, string correlationId);

        public IEnumerable<HoldingsServiceResponse> FilterHoldingsByLatestExpiry(IEnumerable<HoldingsServiceResponse> holdingsServiceResponse);
    }
}
