using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.Request
{
    public class PermitRequest
    {
        [JsonPropertyName("products")]
        public List<Product> Products { get; set; }

        [JsonPropertyName("userPermits")]
        public List<UserPermit> UserPermits { get; set; }
    }
}
