using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.Holdings
{
    public class HoldingsServiceResponse
    {
        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; }

        [JsonPropertyName("productTitle")]
        public string ProductTitle { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime ExpiryDate { get; set; }

        [JsonPropertyName("datasets")]
        public List<Dataset> Datasets { get; set; }
    }
}