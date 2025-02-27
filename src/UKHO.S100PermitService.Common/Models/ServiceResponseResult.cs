using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class ServiceResponseResult<T> : Result<T>
    {
        private ServiceResponseResult(T value, HttpStatusCode statusCode, string origin = null, ErrorResponse errorResponse = null)
            : base(value, statusCode, origin, errorResponse) { }

        public static ServiceResponseResult<T> Success(T value) => new(value, HttpStatusCode.OK);

        public static ServiceResponseResult<T> Failure(HttpStatusCode httpStatusCode, string origin, ErrorResponse? errorResponse = null) => new(default, httpStatusCode, origin, errorResponse);
    }
}