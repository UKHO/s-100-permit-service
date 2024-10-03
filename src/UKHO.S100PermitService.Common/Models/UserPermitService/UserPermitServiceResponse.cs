namespace UKHO.S100PermitService.Common.Models.UserPermitService
{
    public class UserPermitServiceResponse
    {
        public int LicenceId { get; set; }
        public List<UserPermit> UserPermits { get; set; }
    }
}
