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

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(PermitServiceException ex)
            {
                await HandleExceptionAsync(context, ex, ex.EventId.Id);
            }
            catch(Exception ex)
            {
                await HandleExceptionAsync(context, ex, EventIds.UnhandledException.ToEventId().Id);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex, int eventId)
        {
            var correlationId = context.Request.Headers[Constants.XCorrelationIdHeaderKey].FirstOrDefault()!;
            _logger.LogError(new EventId(eventId), ex, "Message: {ex.Message}", ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = "Unhandled controller exception",
                Extensions = { ["CorrelationId"] = correlationId }
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
