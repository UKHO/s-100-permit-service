using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.API.Middleware
{
    [ExcludeFromCodeCoverage]
    public static class CorrelationIdMiddleware
    {
        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";

        public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder builder)
        {
            return builder.Use(async (context, func) =>
            {
                var correlationId = context.Request.Headers[XCorrelationIdHeaderKey].FirstOrDefault();

                if(string.IsNullOrEmpty(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString();
                    context.Request.Headers[XCorrelationIdHeaderKey] = correlationId;
                }
                context.Response.Headers[XCorrelationIdHeaderKey] = correlationId;

                await func();
            });
        }
    }
}
