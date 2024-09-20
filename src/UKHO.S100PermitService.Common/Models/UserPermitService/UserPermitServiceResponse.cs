namespace UKHO.S100PermitService.Common.Models.UserPermitService
{
    public class UserPermitServiceResponse
    {
        public string LicenceId { get; set; }
        public List<UserPermit> UserPermits { get; set; }
    }
}
