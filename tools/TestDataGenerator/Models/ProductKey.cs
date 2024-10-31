using System.Text.Json.Serialization;

namespace TestDataGenerator.Models
{
    public class ProductKey
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("decryptedKey")]
        public string DecryptedKey { get; set; }

        [JsonPropertyName("hardwareId")]
        public string HardwareId { get; set; }
    }
}