using Azure.Security.KeyVault.Secrets;
using Azure;
using Azure.Identity;

namespace MaintainManufacturerKey
{
    internal class SecretUploader
    {
        private readonly SecretClient _client;

        public SecretUploader(string keyVaultUrl)
        {
            _client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        }

        public async Task InsertSecretAsync(string name, string value)
        {
            try
            {
                // TODO - this is horrible! Find a nicer way of doing this?

                await _client.GetSecretAsync(name);
                throw new InvalidOperationException($"Secret '{name}' already exists.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                await _client.SetSecretAsync(name, value);
            }
        }

        public async Task UpsertSecretAsync(string name, string value)
        {
            await _client.SetSecretAsync(name, value);
        }
    }
}