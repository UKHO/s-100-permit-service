namespace MaintainManufacturerKey.Configuration
{
    public class AppSettings
    {
        public required string KeyVaultUrl { get; set; }

        public required string FilePath { get; set; }

        public required string ErrorListFilePath { get; set; }

        public bool EventSourceLogging { get; set; }

        public required string ManufacturerIdColumnName { get; set; }

        public required string ManufacturerKeyColumnName { get; set; }

        public string? Password { get; set; }

        public int MaxRowsToSearchForHeader { get; set; } = 50;
    }
}