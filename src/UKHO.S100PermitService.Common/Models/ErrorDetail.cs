using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models
{
    public class ErrorDetail
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ErrorDetail other)
            {
                return string.Equals(Source, other.Source) && string.Equals(Description, other.Description);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Description);
        }
    }
}
