using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class PermitServiceResult : Result<Stream>
    {
        private PermitServiceResult(Stream value, HttpStatusCode statusCode, ErrorResponse errorResponse = null)
            : base(value, statusCode, errorResponse){}

        public static PermitServiceResult Success(Stream value) => new(value, HttpStatusCode.OK);

        public static PermitServiceResult BadRequest(ErrorResponse errorResponse) => new(null, HttpStatusCode.BadRequest, errorResponse);

        public static PermitServiceResult InternalServerError() => new(null, HttpStatusCode.InternalServerError);
    }
}
