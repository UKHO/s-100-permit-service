using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UKHO.S100PermitService.Common.Clients;

namespace UKHO.S100PermitService.API.HealthCheck
{
    public class KeyVaultSecretsHealthCheck : IHealthCheck
    {
        private readonly KeyVaultSecretClient _secretClient;
        private readonly string _secretName;

        public KeyVaultSecretsHealthCheck(string secretName, KeyVaultSecretClient secretClient)
        {
            _secretClient = secretClient;
            _secretName = secretName;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _ = await _secretClient.GetSecretAsync(_secretName);
                return HealthCheckResult.Healthy("Key Vault secret is accessible.");
            }
            catch (Azure.RequestFailedException ex)
            {
                return HealthCheckResult.Unhealthy("Key Vault secret is not accessible.", ex);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("An unexpected error occurred while accessing Key Vault secret.", ex);
            }
        }
    }
}
