using Microsoft.AspNetCore.Mvc;

namespace UKHO.S100PermitService.API.Controllers
{
    public abstract class BaseController<T> : ControllerBase
    {

        private readonly IHttpContextAccessor httpContextAccessor;
        protected new HttpContext HttpContext => httpContextAccessor.HttpContext!;

        protected BaseController(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }
    }
}