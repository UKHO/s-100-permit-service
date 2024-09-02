using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Newtonsoft.Json.Serialization;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common.Configuration;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args)
    {
        EventHubLoggingConfiguration eventHubLoggingConfiguration;
        IHttpContextAccessor httpContextAccessor = new HttpContextAccessor();
        var builder = WebApplication.CreateBuilder(args);
        IConfiguration configuration = builder.Configuration;

#if DEBUG
        //Add development overrides configuration
        builder.Configuration.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif
        builder.Configuration.AddEnvironmentVariables();

        var kvServiceUri = configuration["KeyVaultSettings:ServiceUri"];
        if(!string.IsNullOrWhiteSpace(kvServiceUri))
        {
            var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
            new DefaultAzureCredentialOptions()));
            builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }

#if DEBUG
        //create the logger and setup of sinks, filters and properties
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("Logs/UKHO.S100PermitService.API-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
            .CreateLogger();
#endif
        eventHubLoggingConfiguration = configuration.GetSection("EventHubLoggingConfiguration").Get<EventHubLoggingConfiguration>()!;

        builder.Host.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            if(!string.IsNullOrWhiteSpace(eventHubLoggingConfiguration.ConnectionString))
            {
                void ConfigAdditionalValuesProvider(IDictionary<string, object> additionalValues)
                {
                    if(httpContextAccessor.HttpContext != null)
                    {
                        additionalValues["_Environment"] = eventHubLoggingConfiguration.Environment;
                        additionalValues["_System"] = eventHubLoggingConfiguration.System;
                        additionalValues["_Service"] = eventHubLoggingConfiguration.Service;
                        additionalValues["_NodeName"] = eventHubLoggingConfiguration.NodeName;
                        additionalValues["_RemoteIPAddress"] = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                        additionalValues["_User-Agent"] = httpContextAccessor.HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;
                        additionalValues["_AssemblyVersion"] = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
                        additionalValues["_X-Correlation-ID"] = httpContextAccessor.HttpContext.Request.Headers?[CorrelationIdMiddleware.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;
                    }
                }
                ConfigureLogging(logging, eventHubLoggingConfiguration);
            }

            void ConfigureLogging(ILoggingBuilder logging, EventHubLoggingConfiguration eventHubLoggingConfiguration) => logging.AddEventHub(config =>
            {
                config.Environment = eventHubLoggingConfiguration.Environment;
                config.DefaultMinimumLogLevel =
                    (LogLevel)Enum.Parse(typeof(LogLevel), eventHubLoggingConfiguration.MinimumLoggingLevel, true);
                config.MinimumLogLevels["UKHO"] =
                    (LogLevel)Enum.Parse(typeof(LogLevel), eventHubLoggingConfiguration.UkhoMinimumLoggingLevel, true);
                config.EventHubConnectionString = eventHubLoggingConfiguration.ConnectionString;
                config.EventHubEntityPath = eventHubLoggingConfiguration.EntityPath;
                config.System = eventHubLoggingConfiguration.System;
                config.Service = eventHubLoggingConfiguration.Service;
                config.NodeName = eventHubLoggingConfiguration.NodeName;
                config.AdditionalValuesProvider = ConfigAdditionalValuesProvider;
            });
        });

        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddConsole();
            loggingBuilder.AddDebug();
            loggingBuilder.AddSerilog();
            loggingBuilder.AddAzureWebAppDiagnostics();
        });

        // The following line enables Application Insights telemetry collection.
        var options = new ApplicationInsightsServiceOptions { ConnectionString = configuration.GetValue<string>("ApplicationInsights:ConnectionString") };
        builder.Services.AddApplicationInsightsTelemetry(options);

        // Add services to the container.
        builder.Logging.AddAzureWebAppDiagnostics();
        builder.Services.AddApplicationInsightsTelemetry();

        builder.Services.AddControllers(o =>
        {
            o.AllowEmptyInputInBodyModelBinding = true;
        }).AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });

        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddControllers();

        var app = builder.Build();
        app.UseHttpsRedirection();
        app.MapControllers();
        app.Run();
    }
}