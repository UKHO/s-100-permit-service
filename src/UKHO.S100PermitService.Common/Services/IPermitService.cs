using System.Net;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        Task<(HttpStatusCode httpStatusCode, Stream stream)> CreatePermitAsync(int licenceId, CancellationToken cancellationToken, string correlationId);
    }
}