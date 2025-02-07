using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class Result<T>
    {
        public T Value { get; }
        public HttpStatusCode StatusCode { get; }
        public ErrorResponse ErrorResponse { get; }

        protected Result(T value, HttpStatusCode statusCode, ErrorResponse errorResponse = null)
        {
            Value = value;
            StatusCode = statusCode;
            ErrorResponse = errorResponse;
        }

        public bool IsSuccess => StatusCode == HttpStatusCode.OK;
    }
}