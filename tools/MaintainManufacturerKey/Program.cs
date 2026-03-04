using Azure.Core.Diagnostics;
using Azure.Security.KeyVault.Secrets;
using CommandLine;
using MaintainManufacturerKey.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace MaintainManufacturerKey
{
    internal static class Program
    {
        private const string Insert = "insert";
        private const string AppSettingsFile = "appsettings.json";
        private static string kvUrl = string.Empty;

        private static string filePath = string.Empty;

        private static string errorListFilePath = string.Empty;

        private static async Task<int> Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
               .AddJsonFile(AppSettingsFile, false)
               .Build();

            var settings = config.Get<AppSettings>();

            if(settings == null)
            {
                Log.Error("Could not read or parse appsettings.json. Ensure the file exists in the application directory, contains valid JSON, and can be bound to the AppSettings configuration.");

                return 1;
            }

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            // Enable Azure SDK HTTP request/response logging if configured
            using var listener = settings.EventSourceLogging 
                ? AzureEventSourceListener.CreateConsoleLogger() 
                : null;

            try
            {
                Log.Information("S-100 Permit Service Manufacturer Key Maintenance");
                Log.Information("Ctrl+C to exit at any time.");
                int result;
                if(args.Length == 0)
                {
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        Console.WriteLine("\nCtrl+C pressed. Exiting...");
                        Environment.Exit(0);
                    };

                    do
                    {
                        // Reload settings from appsettings.json on each run
                        config = new ConfigurationBuilder()
                            .AddJsonFile(AppSettingsFile, false)
                            .Build();
                        settings = config.Get<AppSettings>() ?? settings;

                        kvUrl = settings.GetKVUrlValue();
                        filePath = settings.GetFilePathValue();
                        errorListFilePath = settings.GetErrorListFilePathValue();

                        await RunInteractive();
                        Console.WriteLine();
                        Console.WriteLine("Run again? (Press any key to continue, Ctrl+C to exit)");
                        Console.ReadKey(intercept: true);
                    }
                    while(true);
                }

                // Load settings for command-line mode
                kvUrl = settings.GetKVUrlValue();
                filePath = settings.GetFilePathValue();
                errorListFilePath = settings.GetErrorListFilePathValue();

                result = await Parser.Default.ParseArguments<Options>(args)
                        .MapResult(RunWithOptions, errs => Task.FromResult(1));

                return result;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "A fatal error occurred");
                return 1;
            }

            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static async Task<int> RunWithOptions(Options opts)
        {
            filePath = opts.FilePath.Trim().Trim('"');

            if(!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return 1;
            }

            var uploader = new SecretUploader(kvUrl);
            var reader = new KeyListReader();
            var data = reader.ReadFile(filePath);

            Log.Information("Starting {Operation} operation for {RecordCount} records...", Insert, data.Count());

            List<SecretChangeRecord> alreadyExistingSecrets = [];

            foreach(var (manufacturerId, manufacturerKey) in data)
            {
                try
                {
                    await uploader.InsertSecretAsync(manufacturerId, manufacturerKey, alreadyExistingSecrets);
                    Log.Information("Inserted {ManufacturerId}", manufacturerId);
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Error on key {ManufacturerId}", manufacturerId);
                }
            }

            var existingSecretsPath = string.IsNullOrWhiteSpace(errorListFilePath)
                ? "ExistingSecrets.csv"
                : errorListFilePath;

            WriteListToCSV(alreadyExistingSecrets, existingSecretsPath);

            return 0;
        }

        private static void WriteListToCSV(IEnumerable<SecretChangeRecord> secrets, string filePath)
        {
            if(!secrets.Any())
            {
                Log.Information("No existing secrets to write to CSV.");
                return;
            }
            // Check if file exists and append date if needed
            if(File.Exists(filePath))
            {
                var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                filePath = Path.Combine(directory, $"{fileNameWithoutExtension}_{timestamp}{extension}");
                Log.Information("File already exists. Creating new file: {FilePath}", filePath);
            }

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
            var records = new List<object>();
            foreach(var s in secrets)
            {
                Log.Warning("Secret {SecretName} already exists. Old value: {OldValue}, New value: {NewValue}", s.Name, s.OldValue, s.NewValue);
                records.Add(new { s.Name, s.OldValue, s.NewValue });
            }

            csv.WriteRecords(records);
        }

        private static async Task<int> RunInteractive()
        {
            Console.WriteLine("No command line arguments supplied. Entering interactive mode.");

            var options = PromptForOptions();

            if(options == null)
            {
                return 1;
            }

            return await RunWithOptions(options);
        }
        private static Options? PromptForOptions()
        {
            Console.Write("Enter path to the Excel or CSV file or leave blank to use appsettings.json value: ");
            var input = Console.ReadLine()?.Trim().Trim('"');
            if(!string.IsNullOrWhiteSpace(input))
            {
                filePath = input;
            }

            return new Options
            {
                FilePath = filePath
            };
        }
    }
}
