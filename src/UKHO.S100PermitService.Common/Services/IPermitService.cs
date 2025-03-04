using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        Task<PermitServiceResult> ProcessPermitRequestAsync(PermitRequest permitRequest, string correlationId, CancellationToken cancellationToken);
    }
}