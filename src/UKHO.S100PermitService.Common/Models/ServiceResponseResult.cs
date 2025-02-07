using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class ServiceResponseResult<T> : Result<T>
    {
        public new ErrorResponse ErrorResponse { get; }

        private ServiceResponseResult(T value, HttpStatusCode statusCode, ErrorResponse errorResponse = null)
            : base(value, statusCode, errorResponse)
        {
            ErrorResponse = errorResponse;
        }

        public static ServiceResponseResult<T> Success(T value) => new(value, HttpStatusCode.OK);

        public static ServiceResponseResult<T> BadRequest(ErrorResponse errorResponse) => new(default, HttpStatusCode.BadRequest, errorResponse);
    }
}