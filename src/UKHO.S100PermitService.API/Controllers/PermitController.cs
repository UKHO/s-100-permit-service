﻿using Microsoft.AspNetCore.Mvc;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermitController : BaseController<PermitController>
    {
        private readonly ILogger<PermitController> _logger;
        private readonly IPermitService _permitService;
        private readonly IPermitReaderWriter _permitReaderWriter;
        private readonly IHoldingsService _holdingsService;

        public PermitController(IHttpContextAccessor httpContextAccessor,
                                    ILogger<PermitController> logger,
                                    IPermitService permitService,
                                    IPermitReaderWriter permitReaderWriter,
                                    IHoldingsService holdingsService)
        : base(httpContextAccessor)
        {
            _logger = logger;
            _permitService = permitService;
            _permitReaderWriter = permitReaderWriter;
            _holdingsService = holdingsService;
        }

        [HttpGet]
        [Route("/permits/{licenceId}")]
        public virtual async Task<IActionResult> GeneratePermits(int licenceId)
        {
            _logger.LogInformation(EventIds.GeneratePermitStarted.ToEventId(), "Generate Permit API call started.");

            await _permitService.CreatePermit(licenceId, GetCurrentCorrelationId());

            _logger.LogInformation(EventIds.GeneratePermitEnd.ToEventId(), "Generate Permit API call end.");

            return Ok();
        }
    }
}