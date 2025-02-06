using System.Diagnostics.CodeAnalysis;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.Services
{
    [ExcludeFromCodeCoverage]
    public class UserPermitService : IUserPermitService
    {
        private readonly IUserPermitValidator _userPermitValidator;

        public UserPermitService(IUserPermitValidator userPermitValidator)
        {
            _userPermitValidator = userPermitValidator ?? throw new ArgumentNullException(nameof(userPermitValidator));
        }

        /// <summary>
        /// Validate User Permit Number (UPN) for any validation failures.
        /// </summary>
        /// <param name="userPermitServiceResponse">User Permit Number (UPN) details.</param>
        /// <exception cref="PermitServiceException">When validation failed then PermitServiceException exception will be thrown.</exception>
        public void ValidateUpnsAndChecksum(UserPermitServiceResponse userPermitServiceResponse)
        {
            var result = _userPermitValidator.Validate(userPermitServiceResponse);

            if(!result.IsValid)
            {
                var errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));

                throw new PermitServiceException(EventIds.UpnLengthOrChecksumValidationFailed.ToEventId(), "Validation failed for Licence Id: {licenceId} | Error Details: {errorMessage}", userPermitServiceResponse.LicenceId, errorMessage);
            }
        }
    }
}