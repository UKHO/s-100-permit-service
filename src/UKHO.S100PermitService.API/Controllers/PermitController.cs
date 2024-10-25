using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Events;
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
        /// Generate S100 standard PERMIT.XML file for the respective User Permit Number (UPN) for a given licence and provides the compressed Zip containing PERMIT.XML files.
        /// </remarks>
        /// <param name="licenceId">Requested licence id.</param>
        /// <response code="200">Compressed zip containing PERMIT.XML.</response>
        /// <response code="204">NoContent - when dependent services responded with empty response.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorized - either you have not provided any credentials, or your credentials are not recognized.</response>
        /// <response code="403">Forbidden - you have been authorized, but you are not allowed to access this resource.</response>
        /// <response code="404">NotFound - when invalid or non exists licence Id requested.</response>
        /// <response code="429">You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">InternalServerError - exception occurred.</response>
        [HttpGet]
        [Route("/permits/{licenceId}")]
        [Authorize(Policy = PermitServiceConstants.PermitServicePolicy)]
        public virtual async Task<IActionResult> GeneratePermits(int licenceId)
        {
            _logger.LogInformation(EventIds.GeneratePermitStarted.ToEventId(), "GeneratePermit API call started.");

            var (httpStatusCode, stream) = await _permitService.ProcessPermitRequestAsync(licenceId, GetRequestCancellationToken(), GetCorrelationId());

            _logger.LogInformation(EventIds.GeneratePermitCompleted.ToEventId(), "GeneratePermit API call completed.");

            return httpStatusCode == HttpStatusCode.OK ? File(stream, PermitServiceConstants.ZipContentType, PermitZipFileName) : StatusCode((int)httpStatusCode);
        }
    }
}