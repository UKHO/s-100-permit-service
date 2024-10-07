using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<PermitController> _logger;
        private readonly IPermitService _permitService; 
        private readonly IManufacturerKeyService _manufacturerKeyService;

        public PermitController(IHttpContextAccessor httpContextAccessor,
                                    ILogger<PermitController> logger,
                                        IPermitService permitService,
                                        IManufacturerKeyService manufacturerKeyService)
            : base(httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permitService = permitService ?? throw new ArgumentNullException(nameof(permitService));
            _manufacturerKeyService = manufacturerKeyService ?? throw new ArgumentNullException(nameof(manufacturerKeyService));
        }

        [HttpGet]
        [Route("/permits/{licenceId}")]
        [Authorize(Policy = PermitServiceConstants.PermitServicePolicy)]
        public virtual async Task<IActionResult> GeneratePermits(int licenceId)
        {
            _logger.LogInformation(EventIds.GeneratePermitStarted.ToEventId(), "Generate Permit API call started.");

            _manufacturerKeyService.GetManufacturerKeys("A1B2C3");
            _manufacturerKeyService.GetManufacturerKeys("67676");
            await _permitService.CreatePermitAsync(licenceId, GetRequestCancellationToken(), GetCorrelationId());

            _logger.LogInformation(EventIds.GeneratePermitEnd.ToEventId(), "Generate Permit API call end.");

            return Ok();
        }
    }
}