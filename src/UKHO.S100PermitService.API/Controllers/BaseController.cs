using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.API.Controllers
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseController<T> : ControllerBase
    {
        private const string PermitZipFileName = "Permits.zip";
        private readonly IHttpContextAccessor _httpContextAccessor;

        protected BaseController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Get Correlation Id.
        /// </summary>
        /// <remarks>
        /// Correlation Id is Guid based id to track request.
        /// Correlation Id can be found in request headers.
        /// </remarks>
        /// <returns>Correlation Id</returns>
        protected string GetCorrelationId()
        {
            return _httpContextAccessor.HttpContext!.Request.Headers[PermitServiceConstants.XCorrelationIdHeaderKey].FirstOrDefault()!;
        }

        /// <summary>
        /// Get Request Cancellation Token.
        /// </summary>
        /// <remarks>
        /// Cancellation Token can be found in request.
        /// If Cancellation Token is true, Then notifies the underlying connection is aborted thus request operations should be cancelled.
        /// </remarks>
        /// <returns>Cancellation Token</returns>
        protected CancellationToken GetRequestCancellationToken()
        {
            return _httpContextAccessor.HttpContext.RequestAborted;
        }

        /// <summary>
        /// Convert PermitServiceResult to IActionResult based on status code.
        /// </summary>
        /// <param name="permitServiceResult"></param>
        /// <returns>Permit file when success</returns>
        protected IActionResult ToActionResult(PermitServiceResult permitServiceResult)
        {
            if(!string.IsNullOrEmpty(permitServiceResult.ErrorResponse.Origin))
            {
                _httpContextAccessor.HttpContext.Response.Headers.Append(PermitServiceConstants.OriginHeaderKey, permitServiceResult.ErrorResponse.Origin);
            }

            return permitServiceResult.StatusCode switch
            {
                HttpStatusCode.OK => File(permitServiceResult.Value, PermitServiceConstants.ZipContentType, PermitZipFileName),
                HttpStatusCode.BadRequest => BadRequest(permitServiceResult.ErrorResponse),
                HttpStatusCode.Unauthorized => Unauthorized(permitServiceResult.ErrorResponse),
                HttpStatusCode.Forbidden => Forbid(),
                HttpStatusCode.NotFound => NotFound(permitServiceResult.ErrorResponse),
                HttpStatusCode.InternalServerError => StatusCode((int)HttpStatusCode.InternalServerError, permitServiceResult.ErrorResponse),
                _ => StatusCode((int)HttpStatusCode.InternalServerError, permitServiceResult.ErrorResponse)
            };
        }
    }
}