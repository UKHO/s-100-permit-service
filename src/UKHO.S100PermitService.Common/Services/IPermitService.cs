using System.Net;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        Task<(HttpStatusCode httpStatusCode, MemoryStream memoryStream)> CreatePermitAsync(int licenceId, CancellationToken cancellationToken, string correlationId);
    }
}