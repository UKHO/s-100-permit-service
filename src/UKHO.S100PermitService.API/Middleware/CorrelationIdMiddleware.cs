using System.Diagnostics.CodeAnalysis;
using UKHO.S100PermitService.Common;

namespace UKHO.S100PermitService.API.Middleware
{
    [ExcludeFromCodeCoverage]
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var correlationId = httpContext.Request.Headers[Constants.XCorrelationIdHeaderKey].FirstOrDefault();

            if(string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                httpContext.Request.Headers.Append(Constants.XCorrelationIdHeaderKey, correlationId);
            }

            httpContext.Response.Headers.Append(Constants.XCorrelationIdHeaderKey, correlationId);

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