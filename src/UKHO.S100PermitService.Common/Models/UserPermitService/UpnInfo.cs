namespace UKHO.S100PermitService.Common.Models.UserPermitService
{
    public class UpnInfo
    {
        public string MId { get; set; }
        public string HardwareId { get; set; }
        public string EncryptedHardwareId { get; set; }
        public string Crc32 { get; set; }
        public string Upn { get; set; }
    }
}