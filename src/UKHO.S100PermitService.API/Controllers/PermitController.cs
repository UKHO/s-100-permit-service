using Microsoft.AspNetCore.Mvc;
using UKHO.S100PermitService.Common.Enum;

namespace UKHO.S100PermitService.API.Controllers
{
    [ApiController]
    public class PermitController : BaseController<PermitController>
    {
        private const string LicenceId = "/permits/{licenceId}";
        private readonly ILogger<PermitController> _logger;
        public PermitController(IHttpContextAccessor httpContextAccessor,
                                ILogger<PermitController> logger)
        : base(httpContextAccessor)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route(LicenceId)]
        public virtual async Task<IActionResult> GeneratePermits(int licenceId)
        {
            _logger.LogInformation(EventIds.GeneratePermitStart.ToEventId(), "User permit api call started.");

            await Task.CompletedTask;

            _logger.LogInformation(EventIds.GeneratePermitEnd.ToEventId(), "User permit api call end.");

            return Ok();
        }
    }
}
