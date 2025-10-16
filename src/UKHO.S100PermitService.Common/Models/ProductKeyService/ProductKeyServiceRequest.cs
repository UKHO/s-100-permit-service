using System.Text.Json.Serialization;
namespace UKHO.S100PermitService.Common.Models.ProductKeyService
{
    public class ProductKeyServiceRequest
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; }

        [JsonPropertyName("edition")]
        public string Edition { get; set; }
    }
}