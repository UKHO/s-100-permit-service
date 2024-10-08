using FluentValidation.Results;
using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validations
{
    public interface IUserPermitValidator
    {
        ValidationResult Validate(UserPermitServiceResponse userPermits);
    }
}
