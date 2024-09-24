using Newtonsoft.Json;

namespace UKHO.S100PermitService.Common.Models.ProductKeyService
{
    public class ProductKeyServiceResponse
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("edition")]
        public string Edition { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }
    }
}