using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.API.Controllers
{
    [ExcludeFromCodeCoverage]
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