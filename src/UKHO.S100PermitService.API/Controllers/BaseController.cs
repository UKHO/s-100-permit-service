using Microsoft.AspNetCore.Mvc;

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
    }
}