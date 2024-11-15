using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        Task<PermitServiceResult> ProcessPermitRequestAsync(int licenceId, CancellationToken cancellationToken, string correlationId);
    }
}