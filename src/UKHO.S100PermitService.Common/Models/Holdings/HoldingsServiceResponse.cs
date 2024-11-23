using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.Holdings
{
    public class HoldingsServiceResponse
    {
        [JsonPropertyName("unitName")]
        public string UnitName { get; set; }

        [JsonPropertyName("unitTitle")]
        public string UnitTitle { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime ExpiryDate { get; set; }

        [JsonPropertyName("datasets")]
        public List<Dataset> Datasets { get; set; }
    }
}