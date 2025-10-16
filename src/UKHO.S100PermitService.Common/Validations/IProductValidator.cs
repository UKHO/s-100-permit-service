using FluentValidation.Results;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Validations
{
    public interface IProductValidator
    {
        ValidationResult Validate(Product product);
    }
}