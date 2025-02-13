using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models
{
    public class ErrorResponse
    {
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; set; }
        [JsonPropertyName("errors")]
        public IEnumerable<ErrorDetail> Errors { get; set; }
    }
}
