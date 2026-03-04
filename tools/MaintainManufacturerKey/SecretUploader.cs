using Azure.Security.KeyVault.Secrets;
using Azure;
using Azure.Identity;
using Serilog;

namespace MaintainManufacturerKey
{
    internal class SecretUploader
    {
        private readonly SecretClient _client;

        public SecretUploader(string keyVaultUrl)
        {
            _client = new SecretClient(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeInteractiveBrowserCredential = false,
                }));
        }

        public async Task InsertSecretAsync(string name, string value, ICollection<SecretChangeRecord> existingValues)
        {
            try
            {
                var originalValue = await _client.GetSecretAsync(name);

                // If the secret already exists and has a different value, add it to the list of existing values
                // because we will need to verify the changed values
                if(!originalValue.Value.Value.Equals(value))
                {
                    existingValues.Add(new SecretChangeRecord(name, originalValue.Value.Value, value));
                }
            }
            catch(RequestFailedException ex) when(ex.Status == 404)
            {
                Log.Debug("Creating secret {SecretName}", name);
                await _client.SetSecretAsync(name, value);
            }
        }
    }
}