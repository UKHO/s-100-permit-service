using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;

namespace UKHO.S100PermitService.Common.Models.ProductKeyService
{
    public class ProductKeyServiceHealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        public HealthStatus HealthStatus
        {
            get
            {
                return Status.ToLower() switch
                {
                    "healthy" => HealthStatus.Healthy,
                    "degraded" => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy,
                };
            }
        }
    }
}