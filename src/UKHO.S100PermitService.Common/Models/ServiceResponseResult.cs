using System.Net;

namespace UKHO.S100PermitService.Common.Models
{
    public class ServiceResponseResult<T> : Result<T>
    {
        public new ErrorResponse ErrorResponse { get; }

        private ServiceResponseResult(T value, HttpStatusCode statusCode, ErrorResponse errorResponse = null)
            : base(value, statusCode, errorResponse)
        {
            ErrorResponse = errorResponse;
        }

        public static ServiceResponseResult<T> Success(T value) => new(value, HttpStatusCode.OK);

        public static ServiceResponseResult<T> NoContent() => new(default, HttpStatusCode.NoContent);

        public static ServiceResponseResult<T> NotFound(ErrorResponse errorResponse) => new(default, HttpStatusCode.NotFound, errorResponse);

        public static ServiceResponseResult<T> BadRequest(ErrorResponse errorResponse) => new(default, HttpStatusCode.BadRequest, errorResponse);
    }
}