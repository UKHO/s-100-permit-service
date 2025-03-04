using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.Request;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermitController : BaseController<PermitController>
    {
        private readonly ILogger<PermitController> _logger;
        private readonly IPermitService _permitService;

        public PermitController(IHttpContextAccessor httpContextAccessor, ILogger<PermitController> logger, IPermitService permitService)
            : base(httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permitService = permitService ?? throw new ArgumentNullException(nameof(permitService));
        }

        /// <summary>
        /// Generates signed PERMIT.XML files for requested products and user permits (UPNs), returning them in a compressed ZIP file.
        /// </summary>
        /// <remarks>
        /// Generate S100 standard PERMIT.XML for the respective User Permit Number (UPN) and products and provides the zip stream containing PERMIT.XML.
        /// If internal service fails, errorResponse will be returned along with origin in response headers as internal service name (for e.g. PKS).
        /// If S100PermitService fails or succeed, response will be returned along with origin in response headers as "PermitService"
        /// </remarks>
        /// <param name="permitRequest">The JSON body containing products and UPNs.</param>
        /// <response code="200">OK - Zip stream containing PERMIT.XML.</response>
        /// <response code="400">Bad Request-Invalid request or invalid product/UPN data.</response>
        /// <response code="401">Unauthorized - Either you have not provided any credentials, or your credentials are not recognized.</response>
        /// <response code="403">Forbidden - you have been authorized, but you are not allowed to access this resource.</response>
        /// <response code="429">Too Many Requests - You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">InternalServerError - exception occurred.</response>
        [HttpPost]
        [Route("/v1/permits/s100")]
        [Authorize(Policy = PermitServiceConstants.PermitServicePolicy)]
        [Produces("application/json")]
        [SwaggerOperation(Description = "<p>It uses the S-100 Part 15 data protection scheme to generate signed PERMIT.XML files for all the User Permit Numbers (UPNs) for the requested licence and returns a compressed zip file containing all these PERMIT.XML files.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(string), description: "<p>OK - Returns permit files in a compressed ZIP file.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(IDictionary<string, string>), description: "<p>Bad request - The request is invalid or one or more of the supplied products or UPNs are invalid.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.Unauthorized, description: "<p>Unauthorised - Either you have not provided valid token, or your token is not recognised.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.Forbidden, description: "<p>Forbidden - You are not authorised to access this resource.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.TooManyRequests, description: "<p>Too Many Requests - Too many requests are being sent in a given amount of time.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.InternalServerError, type: typeof(IDictionary<string, string>), description: "<p>Internal Server Error - An error occurred on the server.</p>")]
        public virtual async Task<IActionResult> GenerateS100Permits([FromBody] PermitRequest permitRequest)
        {
            _logger.LogInformation(EventIds.GeneratePermitStarted.ToEventId(), "GeneratePermit API call started for ProductType {productType}.", PermitServiceConstants.ProductType);

            var permitServiceResult = await _permitService.ProcessPermitRequestAsync(permitRequest, GetCorrelationId(), GetRequestCancellationToken());

            _logger.LogInformation(EventIds.GeneratePermitCompleted.ToEventId(), "GeneratePermit API call completed for ProductType {productType}.", PermitServiceConstants.ProductType);

            return ToActionResult(permitServiceResult);
        }
    }
}