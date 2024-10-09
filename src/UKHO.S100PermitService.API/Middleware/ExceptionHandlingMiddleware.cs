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
                await HandleExceptionAsync(httpContext, permitServiceException, permitServiceException.EventId, permitServiceException.Message, permitServiceException.MessageArguments);
            }
            catch(AesEncryptionException aesEncryptionException)
            {
                await HandleExceptionAsync(httpContext, aesEncryptionException, aesEncryptionException.EventId, aesEncryptionException.Message, aesEncryptionException.MessageArguments);
            }
            catch(Exception exception)
            {
                await HandleExceptionAsync(httpContext, exception, EventIds.UnhandledException.ToEventId(), exception.Message);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception, EventId eventId, string message, params object[] messageArgs)
        {
            httpContext.Response.ContentType = PermitServiceConstants.ContentType;
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            _logger.LogError(eventId, exception, message, messageArgs);

            var correlationId = httpContext.Request.Headers[PermitServiceConstants.XCorrelationIdHeaderKey].FirstOrDefault()!;
            var problemDetails = new ProblemDetails
            {
                Status = httpContext.Response.StatusCode,
                Extensions =
                {
                    ["correlationId"] = correlationId
                }
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}