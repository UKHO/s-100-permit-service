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
using UKHO.S100PermitService.API.Filters;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Factories;
using UKHO.S100PermitService.Common.Handlers;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.API
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        private const string EventHubLoggingConfiguration = "EventHubLoggingConfiguration";
        private const string ProductKeyServiceApiConfiguration = "ProductKeyServiceApiConfiguration";
        private const string DataKeyVaultConfiguration = "DataKeyVaultConfiguration";
        private const string WaitAndRetryConfiguration = "WaitAndRetryConfiguration";
        private const string PermitFileConfiguration = "PermitFileConfiguration";
        private const string AzureAdScheme = "AzureAd";
        private const string AzureAdConfiguration = "AzureAdConfiguration";
        private const string Ukho = "UKHO";

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
            builder.Services.Configure<ProductKeyServiceApiConfiguration>(configuration.GetSection(ProductKeyServiceApiConfiguration));
            builder.Services.Configure<DataKeyVaultConfiguration>(configuration.GetSection(DataKeyVaultConfiguration));
            builder.Services.Configure<WaitAndRetryConfiguration>(configuration.GetSection(WaitAndRetryConfiguration));
            builder.Services.Configure<PermitFileConfiguration>(configuration.GetSection(PermitFileConfiguration));

            var azureAdConfiguration = configuration.GetSection(AzureAdConfiguration).Get<AzureAdConfiguration>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                   .AddJwtBearer(AzureAdScheme, options =>
                   {
                       options.Audience = azureAdConfiguration.ClientId;
                       options.Authority = $"{azureAdConfiguration.MicrosoftOnlineLoginUrl}{azureAdConfiguration.TenantId}";
                       options.Events = new JwtBearerEvents
                       {
                           OnForbidden = context =>
                           {
                               context.Response.Headers.Append(PermitServiceConstants.OriginHeaderKey, PermitServiceConstants.PermitService);
                               return Task.CompletedTask;
                           },
                           OnAuthenticationFailed = context =>
                           {
                               context.Response.Headers.Append(PermitServiceConstants.OriginHeaderKey, PermitServiceConstants.PermitService);
                               return Task.CompletedTask;
                           }
                       };
                   });

            builder.Services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AzureAdScheme)
                .Build())
                .AddPolicy(PermitServiceConstants.PermitServicePolicy, policy => policy.RequireRole(PermitServiceConstants.PermitServicePolicy));

            var productKeyServiceApiConfiguration = builder.Configuration.GetSection(ProductKeyServiceApiConfiguration).Get<ProductKeyServiceApiConfiguration>();
            builder.Services.AddHttpClient<IProductKeyServiceApiClient, ProductKeyServiceApiClient>(client =>
            {
                client.BaseAddress = new Uri(productKeyServiceApiConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromMinutes(productKeyServiceApiConfiguration.RequestTimeoutInMinutes);
            });

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton<IProductKeyServiceAuthTokenProvider, AuthTokenProvider>();
            builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
            builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
            builder.Services.AddSingleton<ISecretClient, KeyVaultSecretClient>();
            builder.Services.AddSingleton<ICertificateClient, KeyVaultCertificateClient>();

            builder.Services.AddScoped<IPermitService, PermitService>();
            builder.Services.AddScoped<IPermitReaderWriter, PermitReaderWriter>();
            builder.Services.AddScoped<IProductKeyService, ProductKeyService>();
            builder.Services.AddScoped<IWaitAndRetryPolicy, WaitAndRetryPolicy>();
            builder.Services.AddScoped<IS100Crypt, S100Crypt>();
            builder.Services.AddScoped<IAesEncryption, AesEncryption>();
            builder.Services.AddScoped<IUserPermitValidator, UserPermitValidator>();
            builder.Services.AddScoped<ISchemaValidator, SchemaValidator>();
            builder.Services.AddScoped<IUriFactory, UriFactory>();
            builder.Services.AddScoped<IPermitRequestValidator, PermitRequestValidator>();
            builder.Services.AddScoped<IProductValidator, ProductValidator>();
            builder.Services.AddScoped<IPermitSignGeneratorService, PermitSignGeneratorService>();
            builder.Services.AddScoped<IDigitalSignatureProvider, DigitalSignatureProvider>();

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
                        Name = Ukho,
                        Email = swaggerConfiguration.Email
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.EnableAnnotations();
                c.OperationFilter<AddHeaderOperationFilter>();

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Please Enter Token",
                    Name = "Authorization"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
    }
}