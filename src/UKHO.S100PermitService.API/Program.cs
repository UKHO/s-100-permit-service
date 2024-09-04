using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using UKHO.Logging.EventHubLogProvider;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Configuration;

namespace UKHO.S100PermitService.API
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureConfiguration(builder);
            ConfigureServices(builder);
            ConfigureSwagger(builder);

            var app = builder.Build();
            if(app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "UKHO S100 Permit Service APIs");
                c.RoutePrefix = "swagger";
            });           

            app.UseCorrelationIdMiddleware();            
            app.UseHeaderPropagation();
            app.UseRouting();

            ConfigureLogging(app);

            app.MapControllers();
            app.Run();
        }

        private static void ConfigureConfiguration(WebApplicationBuilder builder)
        {
#if DEBUG            
            builder.Configuration.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif
            builder.Configuration.AddEnvironmentVariables();

            var configuration = builder.Configuration;
            var kvServiceUri = configuration["KeyVaultSettings:ServiceUri"];

            if(!string.IsNullOrWhiteSpace(kvServiceUri))
            {
                var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(new DefaultAzureCredentialOptions()));
                builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
            }             
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
#if DEBUG
                loggingBuilder.AddSerilog(new LoggerConfiguration()
                                 .WriteTo.File("Logs/UKHO.S100PermitService.API-Logs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                                 .MinimumLevel.Information()
                                 .MinimumLevel.Override("UKHO", LogEventLevel.Debug)
                                 .CreateLogger(), dispose: true);
#endif               
                loggingBuilder.AddAzureWebAppDiagnostics();
            });

            builder.Services.AddHeaderPropagation(options =>
            {
                options.Headers.Add(Constants.XCorrelationIdHeaderKey);
            });

            builder.Services.AddApplicationInsightsTelemetry();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.Configure<EventHubLoggingConfiguration>(builder.Configuration.GetSection("EventHubLoggingConfiguration"));
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        private static void ConfigureLogging(WebApplication webApplication)
        {
            var loggerFactory = webApplication.Services.GetRequiredService<ILoggerFactory>();
            var eventHubLoggingConfiguration = webApplication.Services.GetRequiredService<IOptions<EventHubLoggingConfiguration>>();
            var httpContextAccessor = webApplication.Services.GetRequiredService<IHttpContextAccessor>();

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
                            httpContextAccessor.HttpContext.Request.Headers?[Constants.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;
                    }
                }

                loggerFactory.AddEventHub(config =>
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
        }

        public static void ConfigureSwagger(WebApplicationBuilder builder)
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
    }
}