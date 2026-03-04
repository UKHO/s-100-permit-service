using MaintainManufacturerKey.Configuration;
using Serilog;

namespace MaintainManufacturerKey
{
    internal static class SettingsFileExtensions
    {
        public static string GetKVUrlValue(this AppSettings appSettingsValue)
        {
            if(string.IsNullOrWhiteSpace(appSettingsValue.KeyVaultUrl))
            {
                Log.Error("KeyVaultUrl is missing in appsettings.json");
                return string.Empty;
            }
            return appSettingsValue.KeyVaultUrl;
        }

        public static string GetFilePathValue(this AppSettings appSettingsValue)
        {
            if(string.IsNullOrWhiteSpace(appSettingsValue.FilePath))
            {
                Log.Error("FilePath is missing in appsettings.json");
                return string.Empty;
            }
            return appSettingsValue.FilePath;
        }

        public static string GetErrorListFilePathValue(this AppSettings appSettingsValue)
        {
            if(string.IsNullOrWhiteSpace(appSettingsValue.ErrorListFilePath))
            {
                Log.Error("ErrorListFilePath is missing in appsettings.json");
                return string.Empty;
            }
            return appSettingsValue.ErrorListFilePath;
        }
    }
}
