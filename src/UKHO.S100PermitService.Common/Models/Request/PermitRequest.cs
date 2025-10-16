using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.Request
{
    public class PermitRequest
    {
        [JsonPropertyName("products")]
        public IEnumerable<Product> Products { get; set; }

        [JsonPropertyName("userPermits")]
        public IEnumerable<UserPermit> UserPermits { get; set; }
    }
}
