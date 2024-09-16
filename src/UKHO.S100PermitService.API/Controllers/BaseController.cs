using Microsoft.AspNetCore.Mvc;
using UKHO.S100PermitService.Common;

namespace UKHO.S100PermitService.API.Controllers
{
    public abstract class BaseController<T> : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected new HttpContext HttpContext => _httpContextAccessor.HttpContext!;

        protected BaseController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected string GetCurrentCorrelationId()
        {
            return _httpContextAccessor.HttpContext!.Request.Headers[Constants.XCorrelationIdHeaderKey].FirstOrDefault()!;
        }
    }
}