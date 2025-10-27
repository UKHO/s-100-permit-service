using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;

namespace UKHO.S100PermitService.API.HealthCheck
{
    public class KeyVaultSecretsHealthCheck : IHealthCheck
    {
        private readonly KeyVaultSecretClient _secretClient;
        private readonly string _secretName;

        public KeyVaultSecretsHealthCheck(KeyVaultSecretClient secretClient, IOptions<DataKeyVaultConfiguration> dataKeyVaultConfiguration)
        {
            _secretClient = secretClient;
            _secretName = dataKeyVaultConfiguration.Value.DsPrivateKey;
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
