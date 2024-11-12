using System.Net;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        Task<(HttpResponseMessage httpResponseMessage, Stream stream)> ProcessPermitRequestAsync(int licenceId, CancellationToken cancellationToken, string correlationId);
    }
}