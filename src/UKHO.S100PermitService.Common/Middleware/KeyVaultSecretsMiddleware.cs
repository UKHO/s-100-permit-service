using Microsoft.AspNetCore.Http;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.Middleware
{
    public class KeyVaultSecretsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IKeyVaultSecretService _keyVaultSecretService;

        public KeyVaultSecretsMiddleware(RequestDelegate next, IKeyVaultSecretService keyVaultSecretService)
        {
            _next = next;
            _keyVaultSecretService = keyVaultSecretService;
        }

        public async Task InvokeAsync(HttpContext context)
        {            
            _keyVaultSecretService.RefreshSecrets();            
            await _next(context);
        }
    }
}
