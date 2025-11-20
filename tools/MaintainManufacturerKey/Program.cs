using CommandLine;
using MaintainManufacturerKey.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace MaintainManufacturerKey
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("S-100 Permit Service Manufacturer Key Maintenance");

                return await Parser.Default.ParseArguments<Options>(args).MapResult(RunWithOptions, errs => Task.FromResult(1));
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

        private static async Task<int> RunWithOptions(Options opts)
        {
            if (!File.Exists(opts.FilePath))
            {
                Console.WriteLine($"File not found: {opts.FilePath}");
                return 1;
            }

            if (opts.Operation != "insert" && opts.Operation != "upsert")
            {
                Console.WriteLine("Operation must be 'insert' or 'upsert'");
                return 1;
            }

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();

            var settings = config.Get<AppSettings>();

            if (settings == null)
            {
                Log.Error("Could not read appsettings.json");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(settings.KeyVaultUrl))
            {
                Log.Error("KeyVaultUrl is missing in appsettings.json");
                return 1;
            }

            var uploader = new SecretUploader(settings.KeyVaultUrl);
            var reader = new KeyListReader();
            var data = reader.ReadFile(opts.FilePath);

            // TODO Validate that the key/secret are valid

            Log.Information($"Starting {opts.Operation} operation for {data.Count()} records...");

            foreach (var (manufacturerId, manufacturerKey) in data)
            {
                try
                {
                    if (opts.Operation == "insert")
                    {
                        await uploader.InsertSecretAsync(manufacturerId, manufacturerKey);
                        Log.Information($"Inserted: {manufacturerId}");
                    }
                    else if (opts.Operation == "upsert")
                    {
                        await uploader.UpsertSecretAsync(manufacturerId, manufacturerKey);
                        Log.Information($"Upserted: {manufacturerId}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error on key {manufacturerId}: {ex.Message}");
                }
            }

            return 0;
        }
    }
}
