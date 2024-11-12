using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.Holdings
{
    public class Dataset
    {
        [JsonPropertyName("datasetName")]
        public string DatasetName { get; set; }

        [JsonPropertyName("datasetTitle")]
        public string DatasetTitle { get; set; }

        [JsonPropertyName("latestEditionNumber")]
        public int LatestEditionNumber { get; set; }

        [JsonPropertyName("latestUpdateNumber")]
        public int LatestUpdateNumber { get; set; }
    }
}
