namespace UKHO.S100PermitService.Common.Models.UserPermitService
{
    public class UserPermitServiceResponse
    {
        public string LicenceId { get; set; }
        public List<UserPermit> UserPermits { get; set; }
        public class UserPermit
        {
            public string Title { get; set; }
            public string Upn { get; set; }
        }
    }
}
