using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UKHO.S100PermitService.Common.Models.ProductKeyService
{
    public class ProductKeyServiceHealthResponse
    {
        public string Status { get; init; } = string.Empty;

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