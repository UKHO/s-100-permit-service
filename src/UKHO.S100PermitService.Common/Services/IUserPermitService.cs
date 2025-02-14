using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Services
{
    public interface IUserPermitService
    {
        void ValidateUpnsAndChecksum(UserPermit userPermit);
    }
}