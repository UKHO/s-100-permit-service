using System.Net;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IUserPermitService
    {
        Task<(HttpStatusCode httpStatusCode, UserPermitServiceResponse? userPermitServiceResponse)> GetUserPermitAsync(int licenceId, CancellationToken cancellationToken, string correlationId);
        void ValidateUpnsAndChecksum(UserPermitServiceResponse userPermitServiceResponse);
    }
}
