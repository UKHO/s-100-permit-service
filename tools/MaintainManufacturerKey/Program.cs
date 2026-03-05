using Azure.Core.Diagnostics;
using CommandLine;
using MaintainManufacturerKey.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace MaintainManufacturerKey
{
    internal static class Program
    {
        private const string AppSettingsFile = "appsettings.json";
        private const string DefaultErrorListFileName = "ExistingSecrets.csv";

        private static async Task<int> Main(string[] args)
        {
            var (settings, config) = LoadConfiguration();
            if (settings == null)
            {
                Console.WriteLine("ERROR: Could not read or parse appsettings.json. Ensure the file exists in the application directory, contains valid JSON, and can be bound to the AppSettings configuration.");
                return 1;
            }

            ConfigureLogging();

            using var listener = ConfigureAzureSdkLogging(settings);

            try
            {
                Log.Information("S-100 Permit Service Manufacturer Key Maintenance");

                return args.Length == 0 
                    ? await RunInteractiveModeAsync(config, settings)
                    : await RunCommandLineModeAsync(args, settings);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "A fatal error occurred");
                return 1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static (AppSettings? settings, IConfiguration config) LoadConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(AppSettingsFile, optional: false)
                .Build();

            var settings = config.Get<AppSettings>();
            return (settings, config);
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }

        private static AzureEventSourceListener? ConfigureAzureSdkLogging(AppSettings settings)
        {
            return settings.EventSourceLogging
                ? AzureEventSourceListener.CreateConsoleLogger()
                : null;
        }

        private static async Task<int> RunInteractiveModeAsync(IConfiguration initialConfig, AppSettings initialSettings)
        {
            Log.Information("Ctrl+C to exit at any time.");

            SetupExitHandler();

            var config = initialConfig;
            var settings = initialSettings;

            while (true)
            {
                // Reload settings from appsettings.json on each iteration
                (settings, config) = ReloadConfiguration(settings);

                var context = new ExecutionContext(
                    settings.GetKVUrlValue(),
                    settings.GetFilePathValue(),
                    settings.GetErrorListFilePathValue()
                );

                await ExecuteOperationAsync(context);

                Console.WriteLine();
                Console.WriteLine("Run again? (Press any key to continue, Ctrl+C to exit)");
                Console.ReadKey(intercept: true);
            }
        }

        private static (AppSettings settings, IConfiguration config) ReloadConfiguration(AppSettings fallback)
        {
            try
            {
                var (settings, config) = LoadConfiguration();
                return (settings ?? fallback, config);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to reload configuration, using previous settings");
                return (fallback, null!);
            }
        }

        private static void SetupExitHandler()
        {
            Console.CancelKeyPress += (_, _) =>
            {
                Console.WriteLine("\nCtrl+C pressed. Exiting...");
                Environment.Exit(0);
            };
        }

        private static async Task<int> RunCommandLineModeAsync(string[] args, AppSettings settings)
        {
            var context = new ExecutionContext(
                settings.GetKVUrlValue(),
                settings.GetFilePathValue(),
                settings.GetErrorListFilePathValue()
            );

            return await Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    opts => RunWithOptionsAsync(opts, context),
                    _ => Task.FromResult(1)
                );
        }

        private static async Task<string?> ExecuteOperationAsync(ExecutionContext context)
        {
            Console.WriteLine("No command line arguments supplied. Entering interactive mode.");

            var options = PromptForOptions(context.FilePath);
            if (options == null)
            {
                return null;
            }

            await RunWithOptionsAsync(options, context);

            // Return the file path that was actually used
            return options.FilePath;
        }

        private static async Task<int> RunWithOptionsAsync(Options opts, ExecutionContext context)
        {
            var filePath = opts.FilePath.Trim().Trim('"');

            if (!File.Exists(filePath))
            {
                Log.Error("File not found: {FilePath}", filePath);
                return 1;
            }

            var uploader = new SecretUploader(context.KvUrl);
            var dataLoader = () => KeyListReader.ReadFile(filePath);  // Delegate
            var data = dataLoader();  // Invoke it to get the actual data

            Log.Information("Starting insert operation for {RecordCount} records...", data.Count());

            var existingSecrets = await ProcessSecretsAsync(uploader, data);

            var outputPath = DetermineOutputPath(context.ErrorListFilePath);
            WriteSecretsToFile(existingSecrets, outputPath);

            return 0;
        }

        private static async Task<List<SecretChangeRecord>> ProcessSecretsAsync(
            SecretUploader uploader,
            IEnumerable<(string manufacturerId, string manufacturerKey)> data)
        {
            var existingSecrets = new List<SecretChangeRecord>();

            foreach (var (manufacturerId, manufacturerKey) in data)
            {
                try
                {
                    await uploader.InsertSecretAsync(manufacturerId, manufacturerKey, existingSecrets);
                    Log.Information("Inserted {ManufacturerId}", manufacturerId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error on key {ManufacturerId}", manufacturerId);
                }
            }

            return existingSecrets;
        }

        private static string DetermineOutputPath(string configuredPath)
        {
            return string.IsNullOrWhiteSpace(configuredPath)
                ? DefaultErrorListFileName
                : configuredPath;
        }

        private static void WriteSecretsToFile(IEnumerable<SecretChangeRecord> secrets, string filePath)
        {
            if (!secrets.Any())
            {
                Log.Information("No existing secrets with changed values. No file created.");
                return;
            }

            filePath = EnsureUniqueFilePath(filePath);

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            var records = secrets.Select(s =>
            {
                Log.Warning("Secret {SecretName} already exists. Old value: {OldValue}, New value: {NewValue}",
                    s.Name, s.OldValue, s.NewValue);
                return new { s.Name, s.OldValue, s.NewValue };
            });

            csv.WriteRecords(records);

            Log.Information("Written {Count} existing secrets to {FilePath}", secrets.Count(), filePath);
        }

        private static string EnsureUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var newPath = Path.Combine(directory, $"{fileNameWithoutExtension}_{timestamp}{extension}");

            Log.Information("File already exists. Creating new file: {FilePath}", newPath);

            return newPath;
        }

        private static Options? PromptForOptions(string defaultFilePath)
        {
            Console.Write($"Enter path to the Excel or CSV file (or press Enter to use: {defaultFilePath}): ");
            var input = Console.ReadLine()?.Trim().Trim('"');

            var filePath = !string.IsNullOrWhiteSpace(input) ? input : defaultFilePath;

            return new Options { FilePath = filePath };
        }

        private sealed record ExecutionContext(string KvUrl, string FilePath, string ErrorListFilePath);
    }
}
