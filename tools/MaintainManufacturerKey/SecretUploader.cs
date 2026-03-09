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

                // Track newly created secrets with empty OldValue so they can be deleted during undo
                existingValues.Add(new SecretChangeRecord(name, string.Empty, value));
            }
        }

        public async Task<bool> UndoSecretChangeAsync(string name, string oldValue, string newValue)
        {
            try
            {
                // Get the current value to verify it matches what we expect
                var currentSecret = await _client.GetSecretAsync(name);

                if (currentSecret.Value.Value == newValue)
                {
                    // If oldValue is empty, it means the secret was newly created, so delete it
                    if (string.IsNullOrEmpty(oldValue))
                    {
                        Log.Information("Deleting newly created secret: {SecretName}", name);
                        await _client.StartDeleteSecretAsync(name);
                        return true;
                    }
                    else
                    {
                        // Restore the old value
                        Log.Information("Restoring secret {SecretName} from '{NewValue}' to '{OldValue}'", 
                            name, newValue, oldValue);
                        await _client.SetSecretAsync(name, oldValue);
                        return true;
                    }
                }
                else
                {
                    Log.Warning("Secret {SecretName} has been modified since the operation. " +
                        "Expected: '{NewValue}', Current: '{CurrentValue}'. Skipping undo.",
                        name, newValue, currentSecret.Value.Value);
                    return false;
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Log.Warning("Secret {SecretName} not found in Key Vault. It may have already been deleted.", name);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error undoing secret {SecretName}", name);
                return false;
            }
        }
    }
}