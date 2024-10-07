using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.UserPermitService
{
    public class UserPermit
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("upn")]
        public string Upn { get; set; }
    }
}
