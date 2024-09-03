using System.Diagnostics.CodeAnalysis;
using UKHO.S100PermitService.Common;

namespace UKHO.S100PermitService.API.Middleware
{
    [ExcludeFromCodeCoverage]
    public static class CorrelationIdMiddleware
    {
        public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder builder)
        {
            return builder.Use(async (context, func) =>
            {
                var correlationId = context.Request.Headers[Constants.XCorrelationIdHeaderKey].FirstOrDefault();

                if(string.IsNullOrEmpty(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString();
                    context.Request.Headers[Constants.XCorrelationIdHeaderKey] = correlationId;
                }
                context.Response.Headers[Constants.XCorrelationIdHeaderKey] = correlationId;

                await func();
            });
        }
    }
}
