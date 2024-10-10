using System.Net;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IPermitService
    {
        Task<HttpStatusCode> CreatePermitAsync(int licenceId, CancellationToken cancellationToken, string correlationId);

        bool ValidateSchema(string permitXml, string xsdPath);

    }
}