using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.Request
{
    public class UserPermit
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("upn")]
        public string Upn { get; set; }
    }
}