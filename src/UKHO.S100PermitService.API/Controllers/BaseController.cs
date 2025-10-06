using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.API.Controllers
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        protected BaseController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Bound from the X-Correlation-ID header by the MVC model binder.
        [FromHeader(Name = PermitServiceConstants.XCorrelationIdHeaderKey)]
        public string CorrelationId { get; set; }

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
        /// Converts a PermitServiceResult to an appropriate IActionResult based on the status code.
        /// </summary>
        /// <remarks>
        /// This method appends an "Origin" header to the HTTP response, with the value derived from the result's origin if available, or the default PermitService origin if not.
        /// </remarks>
        /// <param name="permitServiceResult">The result of the permit service operation, containing the status code, error response, and value.</param>
        /// <returns>An IActionResult representing the HTTP response, including a permit file if the operation was successful.</returns>
        protected IActionResult ToActionResult(PermitServiceResult permitServiceResult)
        {
            var originHeaderValue = !permitServiceResult.IsSuccess && !string.IsNullOrEmpty(permitServiceResult.Origin)
                ? permitServiceResult.Origin
                : PermitServiceConstants.PermitService;

            _httpContextAccessor.HttpContext.Response.Headers.Append(PermitServiceConstants.OriginHeaderKey, originHeaderValue);

            return permitServiceResult.StatusCode switch
            {
                HttpStatusCode.OK => File(permitServiceResult.Value, PermitServiceConstants.ZipContentType, PermitServiceConstants.PermitZipFileName),
                HttpStatusCode.BadRequest => BadRequest(permitServiceResult.ErrorResponse),
                HttpStatusCode.InternalServerError => StatusCode(StatusCodes.Status500InternalServerError, permitServiceResult.ErrorResponse),
                _ => StatusCode((int)permitServiceResult.StatusCode, null)
            };
        }
    }
}