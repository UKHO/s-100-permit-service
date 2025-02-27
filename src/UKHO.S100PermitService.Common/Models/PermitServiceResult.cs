using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class PermitServiceResult : Result<Stream>
    {
        private PermitServiceResult(Stream value, HttpStatusCode statusCode, string origin = null, ErrorResponse errorResponse = null)
            : base(value, statusCode, origin, errorResponse) { }

        public static PermitServiceResult Success(Stream value) => new(value, HttpStatusCode.OK);
        public static PermitServiceResult Failure(HttpStatusCode httpStatusCode, string origin, ErrorResponse? errorResponse = null) => new(null, httpStatusCode, origin, errorResponse);
    }
}
