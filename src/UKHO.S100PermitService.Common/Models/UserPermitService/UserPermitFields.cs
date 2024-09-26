namespace UKHO.S100PermitService.Common.Models.UserPermitService
{
    public class UserPermitFields
    {
        public string Upn { get; set; }
        public string EncryptedHardwareId { get; set; }
        public string CheckSum { get; set; }
        public string MId { get; set; }
    }
}
