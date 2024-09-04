using System.Diagnostics.CodeAnalysis;
using UKHO.S100PermitService.Common;

namespace UKHO.S100PermitService.API.Middleware
{
    [ExcludeFromCodeCoverage]
    public class CorrelationIdMiddleware
    {
        public const string CorrelationIdKey = "data.correlationId";
        public const string CorrIdKey = "corrid";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            httpContext.Request.EnableBuffering();
            var correlationId = Guid.NewGuid().ToString();
            httpContext.Request.Body.Position = 0;           

            var state = new Dictionary<string, object>
            {
                [Constants.XCorrelationIdHeaderKey] = correlationId!,
            };
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>();
            using(logger.BeginScope(state))
            {
                await _next(httpContext);
            }
        }
    }
}
