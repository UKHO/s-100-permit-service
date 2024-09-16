using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IUserPermitService
    {
        Task<UserPermitServiceResponse> GetUserPermitAsync(int licenceId);
    }
}
