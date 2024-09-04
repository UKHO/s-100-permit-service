using System.Net;
using UKHO.S100PermitService.Common.Enum;
using UKHO.S100PermitService.Common.Exception;

namespace UKHO.S100PermitService.API.Middleware
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch(Exception exception)
            {
                var exceptionType = exception.GetType();
                var correlationId = httpContext!.Request.Headers[Common.Constants.XCorrelationIdHeaderKey].FirstOrDefault()!;

                if(exceptionType == typeof(PermitServiceException))
                {
                    var eventId = (EventIds)((PermitServiceException)exception).EventId.Id;
                    _logger.LogError(eventId.ToEventId(), exception, eventId.ToString() + ". | _X-Correlation-ID : {_X-Correlation-ID}", correlationId);
                }
                else
                {
                    _logger.LogError(EventIds.UnhandledException.ToEventId(), exception, "Exception occured while processing Permit Service API." + " | _X-Correlation-ID : {_X-Correlation-ID}", correlationId);
                }
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
    }
}
