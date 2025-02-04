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
        private const string PermitZipFileName = "Permits.zip";

        private readonly ILogger<PermitController> _logger;
        private readonly IPermitService _permitService;

        public PermitController(IHttpContextAccessor httpContextAccessor, ILogger<PermitController> logger, IPermitService permitService)
            : base(httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permitService = permitService ?? throw new ArgumentNullException(nameof(permitService));
        }

        /// <summary>
        /// Provide Permits for requested licence Id.
        /// </summary>
        /// <remarks>
        /// Generate S100 standard PERMIT.XML for the respective User Permit Number (UPN) for a given licence and provides the zip stream containing PERMIT.XML.
        /// </remarks>
        /// <param name="productType" example="s100">Requested Product type.</param>
        /// <param name="permitRequest">The JSON body containing products and UPNs.</param>
        /// <response code="200">Zip stream containing PERMIT.XML.</response>
        /// <response code="204">NoContent - when dependent services responded with empty response.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorized - either you have not provided any credentials, or your credentials are not recognized.</response>
        /// <response code="403">Forbidden - you have been authorized, but you are not allowed to access this resource.</response>
        /// <response code="404">NotFound - when invalid or non exists licence Id requested.</response>
        /// <response code="429">You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">InternalServerError - exception occurred.</response>
        [HttpPost]
        [Route("/v1/permits/{productType}")]
        [Authorize(Policy = PermitServiceConstants.PermitServicePolicy)]
        [Produces("application/json")]
        [SwaggerOperation(Description = "<p>It uses the S-100 Part 15 data protection scheme to generate signed PERMIT.XML files for all the User Permit Numbers (UPNs) for the requested licence and returns a compressed zip file containing all these PERMIT.XML files.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(string), description: "<p>OK - Returns permit files.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.NoContent, description: "<p>No Content - There are no S100 permits for the licence.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(IDictionary<string, string>), description: "<p>Bad request - could be missing or invalid licenceId, it must be an integer and greater than zero.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.Unauthorized, description: "<p>Unauthorised - either you have not provided valid token, or your token is not recognised.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.Forbidden, description: "<p>Forbidden - you have no permission to use this API.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.NotFound, type: typeof(IDictionary<string, string>), description: "<p>Licence not found (licence not found in response received from Shop Facade UPN Service or Shop Facade Holding Service).</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.TooManyRequests, description: "<p>Too Many Requests.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.InternalServerError, type: typeof(IDictionary<string, string>), description: "<p>Internal Server Error.</p>")]
        public virtual async Task<IActionResult> GeneratePermits(string productType, [FromBody] PermitRequest permitRequest)
        {
            _logger.LogInformation(EventIds.GeneratePermitStarted.ToEventId(), "GeneratePermit API call started.");

            var permitServiceResult = await _permitService.ProcessPermitRequestAsync(productType, permitRequest, GetCorrelationId(), GetRequestCancellationToken());

            _logger.LogInformation(EventIds.GeneratePermitCompleted.ToEventId(), "GeneratePermit API call completed.");

            return permitServiceResult.StatusCode switch
            {
                HttpStatusCode.OK => File(permitServiceResult.Value, PermitServiceConstants.ZipContentType, PermitZipFileName),
                HttpStatusCode.BadRequest => BadRequest(permitServiceResult.ErrorResponse),
                HttpStatusCode.NotFound => NotFound(permitServiceResult.ErrorResponse),
                HttpStatusCode.NoContent => NoContent(),
                _ => StatusCode((int)permitServiceResult.StatusCode)
            };
        }
    }
}