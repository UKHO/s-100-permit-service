using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IUserPermitService
    {
        Task<ServiceResponseResult<UserPermitServiceResponse>> GetUserPermitAsync(int licenceId, string correlationId, CancellationToken cancellationToken);
        void ValidateUpnsAndChecksum(UserPermitServiceResponse userPermitServiceResponse);
    }
}