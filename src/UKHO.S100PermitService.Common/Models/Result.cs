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
        public string Origin { get; set; }

        protected Result(T value, HttpStatusCode statusCode, string origin = null, ErrorResponse errorResponse = null)
        {
            Value = value;
            StatusCode = statusCode;
            ErrorResponse = errorResponse;
            Origin = origin;
        }

        public bool IsSuccess => StatusCode == HttpStatusCode.OK;
    }
}