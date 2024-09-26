using FluentValidation.Results;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validation
{
    public interface IUserPermitValidator
    {
        ValidationResult Validate(UserPermitServiceResponse userPermits);
    }
}
