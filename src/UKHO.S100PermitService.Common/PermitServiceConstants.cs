using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common
{
    [ExcludeFromCodeCoverage]
    public static class PermitServiceConstants
    {
        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";

        public const string PermitServicePolicy = "PermitServiceUser";

        public const string ContentType = "application/json";

        public const string ZipContentType = "application/zip";

        public const string OriginHeaderKey = "origin";

        public const string ProductKeyService = "PKS";

        public const string PermitService = "PermitService";
    }
}