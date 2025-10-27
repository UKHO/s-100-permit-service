using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Net.Http;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.API.HealthCheck
{
    public class ProductKeyServiceHealthCheck(IProductKeyServiceApiClient httpClient) : IHealthCheck
    {

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetHealthCheckAsync(cancellationToken);
            return result switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy("Product Key Service healthy."),
                HealthStatus.Degraded => HealthCheckResult.Degraded("Product Key Service degraded."),
                _ => HealthCheckResult.Unhealthy("Product Key Service unhealthy.")
            };
        }
    }
}
