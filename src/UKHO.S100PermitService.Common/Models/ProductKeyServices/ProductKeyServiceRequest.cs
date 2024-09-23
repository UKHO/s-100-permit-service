using Newtonsoft.Json;

namespace UKHO.S100PermitService.Common.Models.ProductKeyServices
{
    public class ProductKeyServiceRequest
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }
        [JsonProperty("edition")]
        public string Edition { get; set; }
    }
}