using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class PermitServiceResult : Result<Stream>
    {
        public new ErrorResponse ErrorResponse { get; }
        public new HttpStatusCode StatusCode { get; }

        private PermitServiceResult(Stream value, HttpStatusCode statusCode, ErrorResponse errorResponse = null)
            : base(value, statusCode, errorResponse)
        {
            StatusCode = statusCode;
            ErrorResponse = errorResponse;
        }

        public static PermitServiceResult Success(Stream value) => new(value, HttpStatusCode.OK);

        public static PermitServiceResult BadRequest(ErrorResponse errorResponse) => new(null, HttpStatusCode.BadRequest, errorResponse);

        public static PermitServiceResult InternalServerError() => new(null, HttpStatusCode.InternalServerError);
    }
}
