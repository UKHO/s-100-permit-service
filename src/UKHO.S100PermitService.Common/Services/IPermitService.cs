using System.Net;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        Task<(HttpStatusCode httpStatusCode, Stream stream)> ProcessPermitRequestAsync(int licenceId, CancellationToken cancellationToken, string correlationId);
    }
}