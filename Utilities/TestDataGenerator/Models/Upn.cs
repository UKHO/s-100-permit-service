using System.Text.Json.Serialization;

namespace TestData.Models
{
    public class Upn
    {
        [JsonPropertyName("m_Id")]
        public string MId { get; set; }

        [JsonPropertyName("m_Key")]
        public string MKey { get; set; }

        [JsonPropertyName("hw_Id")]
        public string HwId { get; set; }

        [JsonPropertyName("hw_IdEncrypted")]
        public string HwIdEncrypted { get; set; }

        [JsonPropertyName("crc32")]
        public string Crc32 { get; set; }

        [JsonPropertyName("completeUserPermit")]
        public string CompleteUserPermit { get; set; }
    }
}
