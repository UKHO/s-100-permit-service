using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using UKHO.S100PermitService.Common;

namespace UKHO.S100PermitService.API.Controllers
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseController<T> : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        protected BaseController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected string GetCorrelationId()
        {
            return _httpContextAccessor.HttpContext!.Request.Headers[PermitServiceConstants.XCorrelationIdHeaderKey].FirstOrDefault()!;
        }

        protected CancellationToken GetRequestCancellationToken()
        {
            return _httpContextAccessor.HttpContext.RequestAborted;
        }
    }
}