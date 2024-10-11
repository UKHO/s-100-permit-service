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
using System.Reflection;
using UKHO.Logging.EventHubLogProvider;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Handlers;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        private const string HoldingsServiceApiConfiguration = "HoldingsServiceApiConfiguration";
        private const string UserPermitServiceApiConfiguration = "UserPermitServiceApiConfiguration";
        private const string EventHubLoggingConfiguration = "EventHubLoggingConfiguration";
        private const string ProductKeyServiceApiConfiguration = "ProductKeyServiceApiConfiguration";
        private const string ManufacturerKeyVaultConfiguration = "ManufacturerKeyVault";
        private const string WaitAndRetryConfiguration = "WaitAndRetryConfiguration";
        private const string AzureAdScheme = "AzureAd";
        private const string AzureAdConfiguration = "AzureAdConfiguration";

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
                options.Headers.Add(PermitServiceConstants.XCorrelationIdHeaderKey);
            });

            var options = new ApplicationInsightsServiceOptions { ConnectionString = configuration.GetValue<string>("ApplicationInsights:ConnectionString") };
            builder.Services.AddApplicationInsightsTelemetry(options);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddMemoryCache();
            builder.Services.AddDistributedMemoryCache();

            builder.Services.Configure<EventHubLoggingConfiguration>(configuration.GetSection(EventHubLoggingConfiguration));
            builder.Services.Configure<HoldingsServiceApiConfiguration>(configuration.GetSection(HoldingsServiceApiConfiguration));
            builder.Services.Configure<UserPermitServiceApiConfiguration>(configuration.GetSection(UserPermitServiceApiConfiguration));
            builder.Services.Configure<ProductKeyServiceApiConfiguration>(configuration.GetSection(ProductKeyServiceApiConfiguration));
            builder.Services.Configure<ManufacturerKeyVaultConfiguration>(configuration.GetSection(ManufacturerKeyVaultConfiguration));
            builder.Services.Configure<WaitAndRetryConfiguration>(configuration.GetSection(WaitAndRetryConfiguration));

            var azureAdConfiguration = configuration.GetSection(AzureAdConfiguration).Get<AzureAdConfiguration>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                   .AddJwtBearer(AzureAdScheme, options =>
                   {
                       options.Audience = azureAdConfiguration.ClientId;
                       options.Authority = $"{azureAdConfiguration.MicrosoftOnlineLoginUrl}{azureAdConfiguration.TenantId}";
                   });

            builder.Services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AzureAdScheme)
                .Build())
                .AddPolicy(PermitServiceConstants.PermitServicePolicy, policy => policy.RequireRole(PermitServiceConstants.PermitServicePolicy));

            var holdingsServiceApiConfiguration = builder.Configuration.GetSection(HoldingsServiceApiConfiguration).Get<HoldingsServiceApiConfiguration>();
            builder.Services.AddHttpClient<IHoldingsApiClient, HoldingsApiClient>(client =>
            {
                client.BaseAddress = new Uri(holdingsServiceApiConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromMinutes(holdingsServiceApiConfiguration.RequestTimeoutInMinutes);
            });

            var userPermitServiceApiConfiguration = builder.Configuration.GetSection(UserPermitServiceApiConfiguration).Get<UserPermitServiceApiConfiguration>();
            builder.Services.AddHttpClient<IUserPermitApiClient, UserPermitApiClient>(client =>
            {
                client.BaseAddress = new Uri(userPermitServiceApiConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromMinutes(userPermitServiceApiConfiguration.RequestTimeoutInMinutes);
            });

            var productKeyServiceApiConfiguration = builder.Configuration.GetSection(ProductKeyServiceApiConfiguration).Get<ProductKeyServiceApiConfiguration>();
            builder.Services.AddHttpClient<IUserPermitApiClient, UserPermitApiClient>(client =>
            {
                client.BaseAddress = new Uri(productKeyServiceApiConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromMinutes(productKeyServiceApiConfiguration.RequestTimeoutInMinutes);
            });

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton<IHoldingsServiceAuthTokenProvider, AuthTokenProvider>();
            builder.Services.AddSingleton<IUserPermitServiceAuthTokenProvider, AuthTokenProvider>();
            builder.Services.AddSingleton<IProductKeyServiceAuthTokenProvider, AuthTokenProvider>();            
            builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
            builder.Services.AddSingleton<IManufacturerKeyService, ManufacturerKeyService>();

            builder.Services.AddScoped<IPermitService, PermitService>();
            builder.Services.AddScoped<IPermitReaderWriter, PermitReaderWriter>();
            builder.Services.AddScoped<IHoldingsService, HoldingsService>();
            builder.Services.AddScoped<IUserPermitService, UserPermitService>();
            builder.Services.AddScoped<IProductKeyService, ProductKeyService>();
            builder.Services.AddScoped<IWaitAndRetryPolicy,WaitAndRetryPolicy>();
            builder.Services.AddScoped<IS100Crypt, S100Crypt>();
            builder.Services.AddScoped<IAesEncryption, AesEncryption>();
            builder.Services.AddScoped<ISecretClient, KeyVaultSecretClient>();

            builder.Services.AddTransient<IHoldingsApiClient, HoldingsApiClient>();
            builder.Services.AddTransient<IUserPermitApiClient, UserPermitApiClient>();
            builder.Services.AddTransient<IProductKeyServiceApiClient, ProductKeyServiceApiClient>();            
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
                            httpContextAccessor.HttpContext.Request.Headers?[PermitServiceConstants.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;
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