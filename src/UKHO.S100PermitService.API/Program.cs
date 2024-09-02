using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UKHO.Logging.EventHubLogProvider;
using UKHO.S100PermitService.API.Configuration;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common.Configuration;

namespace UKHO.S100PermitService
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
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
            builder.Services.Configure<EventHubLoggingConfiguration>(builder.Configuration.GetSection("EventHubLoggingConfiguration"));

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

            builder.Services.AddEndpointsApiExplorer();
            ConfigureSwagger();

            var app = builder.Build();
            ConfigureLogging(app);

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "UKHO S100 Permit Service APIs");
                c.RoutePrefix = "swagger";
            });
            app.UseHttpsRedirection();
            app.MapControllers();
            app.Run();

            void ConfigureSwagger()
            {
                var swaggerConfiguration = new SwaggerConfiguration();
                builder.Configuration.Bind("Swagger", swaggerConfiguration);
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = swaggerConfiguration.Version,
                        Title = swaggerConfiguration.Title,
                        Description = swaggerConfiguration.Description,
                        Contact = new OpenApiContact
                        {
                            Email = swaggerConfiguration.Email,
                        },
                    });
                });
            }

            void ConfigureLogging(WebApplication app)
            {
                var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
                var eventHubLoggingConfiguration = app.Services.GetRequiredService<IOptions<EventHubLoggingConfiguration>>();
                var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();

                if(!string.IsNullOrWhiteSpace(eventHubLoggingConfiguration?.Value.ConnectionString))
                {
                    void ConfigAdditionalValuesProvider(IDictionary<string, object> additionalValues)
                    {
                        if(httpContextAccessor.HttpContext != null)
                        {
                            additionalValues["_RemoteIPAddress"] = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                            additionalValues["_User-Agent"] = httpContextAccessor.HttpContext.Request.Headers.UserAgent.FirstOrDefault() ?? string.Empty;
                            additionalValues["_AssemblyVersion"] = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
                            additionalValues["_X-Correlation-ID"] =
                                httpContextAccessor.HttpContext.Request.Headers?[CorrelationIdMiddleware.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;
                        }
                    }

                    loggerFactory.AddEventHub(
                                             config =>
                                             {
                                                 config.Environment = eventHubLoggingConfiguration.Value.Environment;
                                                 config.DefaultMinimumLogLevel =
                                                     (LogLevel)Enum.Parse(typeof(LogLevel), eventHubLoggingConfiguration.Value.MinimumLoggingLevel, true);
                                                 config.MinimumLogLevels["UKHO"] =
                                                     (LogLevel)Enum.Parse(typeof(LogLevel), eventHubLoggingConfiguration.Value.UkhoMinimumLoggingLevel, true);
                                                 config.EventHubConnectionString = eventHubLoggingConfiguration.Value.ConnectionString;
                                                 config.EventHubEntityPath = eventHubLoggingConfiguration.Value.EntityPath;
                                                 config.System = eventHubLoggingConfiguration.Value.System;
                                                 config.Service = eventHubLoggingConfiguration.Value.Service;
                                                 config.NodeName = eventHubLoggingConfiguration.Value.NodeName;
                                                 config.AdditionalValuesProvider = ConfigAdditionalValuesProvider;
                                             });
                }
                app.UseCorrelationIdMiddleware();
            }
        }
    }
}