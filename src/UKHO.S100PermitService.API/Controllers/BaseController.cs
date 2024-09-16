using Microsoft.AspNetCore.Mvc;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Events;

namespace UKHO.S100PermitService.API.Controllers
{
    public abstract class BaseController<T> : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly ILogger<T> _logger;
        protected new HttpContext HttpContext => _httpContextAccessor.HttpContext!;

        protected BaseController(IHttpContextAccessor httpContextAccessor, ILogger<T> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected string GetCurrentCorrelationId()
        {
            var correlationId = _httpContextAccessor.HttpContext!.Request.Headers[Constants.XCorrelationIdHeaderKey].FirstOrDefault();
            if(Guid.TryParse(correlationId, out var correlationIdGuid))
            {
                correlationId = correlationIdGuid.ToString();
            }
            else
            {
                correlationId = Guid.Empty.ToString();
                _logger.LogError(EventIds.BadRequest.ToEventId(), null, "_X-Correlation-ID is invalid");
            }
            return correlationId;
        }
    }
}