using Microsoft.AspNetCore.Mvc;
using UKHO.S100PermitService.Common.Enum;
using UKHO.S100PermitService.Common.Helpers;

namespace UKHO.S100PermitService.API.Controllers
{    
    [ApiController]
    public class PermitController : BaseController<PermitController>
    {       
        private readonly ILogger<PermitController> _logger;
        public PermitController(IHttpContextAccessor httpContextAccessor,
                                ILogger<PermitController> logger)
        : base(httpContextAccessor)
        {
            _logger = logger;
        }

        [HttpPost]     
        [Route("/permits/{licenceId}")]
        public virtual async Task<IActionResult> GeneratePermits(int licenceId)
        {
            _logger.LogInformation(EventIds.GeneratePermitStarted.ToEventId(), "Generate Permit API call started | _X-Correlation-ID:{correlationId}", CommonHelper.CorrelationID);

            await Task.CompletedTask;

            _logger.LogInformation(EventIds.GeneratePermitEnd.ToEventId(), "Generate Permit API call end | _X-Correlation-ID:{correlationId}", CommonHelper.CorrelationID);

            return Ok();
        }
    }
}
