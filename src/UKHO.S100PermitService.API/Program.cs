using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using UKHO.Logging.EventHubLogProvider;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        private static void Main(string[] args)
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
            app.UseExceptionHandlingMiddleware();
            app.UseHeaderPropagation();
            app.UseRouting();

            ConfigureLogging(app);

            app.MapControllers();
            app.UseAuthorization();

            app.Run();
        }

        private static void ConfigureConfiguration(WebApplicationBuilder builder)
        {
            builder.Configuration.AddJsonFile("appsettings.json", false, true);
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

            var options = new ApplicationInsightsServiceOptions { ConnectionString = configuration.GetValue<string>("ApplicationInsights:ConnectionString") };
            builder.Services.AddApplicationInsightsTelemetry(options);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.Configure<EventHubLoggingConfiguration>(builder.Configuration.GetSection("EventHubLoggingConfiguration"));

            var azureADConfiguration = new AzureADConfiguration();
            builder.Configuration.Bind("AzureADConfiguration", azureADConfiguration);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                   .AddJwtBearer("AzureAD", options =>
                   {
                       options.Audience = azureADConfiguration.ClientId;
                       options.Authority = $"{azureADConfiguration.MicrosoftOnlineLoginUrl}{azureADConfiguration.TenantId}";
                   });

            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes("AzureAD")
                .Build();
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("PermitServiceUser", policy => policy.RequireRole("PermitServiceUser"));
            });

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddScoped<IPermitService, PermitService>();
            builder.Services.AddScoped<IFileSystem, FileSystem>();
            builder.Services.AddScoped<IPermitReaderWriter, PermitReaderWriter>();
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
                        additionalValues["_RemoteIPAddress"] = httpContextAccessor.HttpContext.Connection.RemoteIpAddress!.ToString();
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
                                                 (LogLevel)Enum.Parse(typeof(LogLevel), eventHubLoggingConfiguration.Value.MinimumLoggingLevel!, true);
                                             config.MinimumLogLevels["UKHO"] =
                                                 (LogLevel)Enum.Parse(typeof(LogLevel), eventHubLoggingConfiguration.Value.UkhoMinimumLoggingLevel!, true);
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