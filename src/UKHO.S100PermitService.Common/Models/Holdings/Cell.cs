using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.Holdings
{
    public class Cell
    {
        [JsonPropertyName("cellCode")]
        public string CellCode { get; set; }

        [JsonPropertyName("cellTitle")]
        public string CellTitle { get; set; }

        [JsonPropertyName("latestEditionNumber")]
        public string LatestEditionNumber { get; set; }

        [JsonPropertyName("latestUpdateNumber")]
        public string LatestUpdateNumber { get; set; }
    }
}
