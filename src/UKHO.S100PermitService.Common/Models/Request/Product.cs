using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.Request
{
    public class Product
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; }

        [JsonPropertyName("editionNumber")]
        public int EditionNumber { get; set; }

        [JsonPropertyName("permitExpiryDate")]
        public DateTime PermitExpiryDate { get; set; }
    }
}
