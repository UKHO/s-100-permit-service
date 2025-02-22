using System.Text.Json.Serialization;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Models.UserPermitService
{
    public class UserPermitServiceResponse
    {
        [JsonPropertyName("licenceId")]
        public int LicenceId { get; set; }

        [JsonPropertyName("userPermits")]
        public List<UserPermit> UserPermits { get; set; }
    }
}