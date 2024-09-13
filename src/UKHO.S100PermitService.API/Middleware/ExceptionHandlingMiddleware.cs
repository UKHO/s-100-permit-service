using Microsoft.AspNetCore.Mvc;
using System.Net;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            catch(PermitServiceException permitServiceException)
            {
                await HandleExceptionAsync(httpContext, permitServiceException, permitServiceException.EventId);
            }
            catch(Exception exception)
            {
                await HandleExceptionAsync(httpContext, exception, EventIds.UnhandledException.ToEventId());
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception, EventId eventId)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if(exception is PermitServiceException permitServiceException)
            {
                _logger.LogError(eventId, exception, permitServiceException.Message, permitServiceException.MessageArguments);
            }
            else
            {
                _logger.LogError(eventId, exception, "Message: {ex.Message}", exception.Message);
            }

            var correlationId = httpContext.Request.Headers[Constants.XCorrelationIdHeaderKey].FirstOrDefault()!;
            var problemDetails = new ProblemDetails
            {
                Status = httpContext.Response.StatusCode,
                Title = "Unhandled controller exception",
                Extensions = { ["CorrelationId"] = correlationId }
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}