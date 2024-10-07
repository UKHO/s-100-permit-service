using UKHO.S100PermitService.Common.Models.UserPermitService;

namespace UKHO.S100PermitService.Common.Validations
{
    public static class UserPermitServiceResponseValidator
    {
        public static bool IsResponseNull(UserPermitServiceResponse userPermitServiceResponse)
        {
            //To handle blank response body
            if(userPermitServiceResponse is null)
            {
                return true;
            }

            var properties = userPermitServiceResponse.GetType().GetProperties();
            return properties.Any(p => p.GetValue(userPermitServiceResponse) == null);
        }
    }
}
