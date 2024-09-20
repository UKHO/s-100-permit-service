using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.Services
{
    public class KeyVaultSecretService : IKeyVaultSecretService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyVaultSecretService> _logger;

        public KeyVaultSecretService(IConfiguration configuration, ILogger<KeyVaultSecretService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void RefreshSecrets()
        {
            try
            {
                var keyVaultEndpoint = _configuration["ManufacturerKeyVault:ServiceUri"];                

                var secretClient = new SecretClient(new Uri(keyVaultEndpoint!), new DefaultAzureCredential());

                if(!string.IsNullOrEmpty(keyVaultEndpoint))
                {                    
                    var rootConfiguration = (IConfigurationRoot)_configuration;                    
                    var secrets = new Dictionary<string, string>();
                    
                    var secretProperties = secretClient.GetPropertiesOfSecrets();
                    foreach(var secretProperty in secretProperties)
                    {
                        var secretName = secretProperty.Name;
                        var secretValue = secretClient.GetSecret(secretName).Value.Value;
                        
                        secrets[secretName] = secretValue;                        
                        var existingSecret = _configuration.GetSection(secretName);
                        if(existingSecret.Exists())
                        {                            
                            existingSecret.Value = secretValue;
                           //// _logger.LogInformation($"Updated the secret '{secretName}' in the configuration.");
                            _logger.LogInformation(EventIds.KeyVaultSecretUpdated.ToEventId(), "Updated the secret: {secret} in the configuration", secretName);
                        }                        
                        else
                        {                            
                            ((IConfigurationBuilder)rootConfiguration).AddInMemoryCollection(new Dictionary<string, string> { [secretName] = secretValue });                            
                            _logger.LogInformation(EventIds.AddedNewKeyVaultSecret.ToEventId(), "Added the new secret: {secretName} to the configuration", secretName);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("The ManufacturerKeyvault:ServiceUrl configuration setting is missing or empty. No secrets were refreshed.");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred while refreshing the secrets from Key Vault.");
            }
        }        

        public List<VaultSecret> FetchSecret(string keyName)
        {
            var keyVaultSecrets = new List<VaultSecret>();
            var keyValue = _configuration[keyName];
            if(!string.IsNullOrEmpty(keyValue))
            {
                keyVaultSecrets.Add(new VaultSecret() { Name = keyName, Value = keyValue });
            }
            ////else
            ////{
            ////    RefreshSecrets();
            ////}
            return keyVaultSecrets;
        }
    }
}