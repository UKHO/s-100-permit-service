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

        public const string ProductType = "s100";

        public const string PermitZipFileName = "Permits.zip";

        public const string DateFormat = "yyyy-MM-ddzzz";

        public const string KeysEnc = "/keys/s100";

        public const string FirstNamespacePrefix = "S100SE";

        public const string SecondNamespace = "http://standards.iso.org/iso/19115/-3/gco/1.0";

        public const string SecondNamespacePrefix = "ns2";

        public const string XmlDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";

        public const string Namespace = "http://www.iho.int/s100/se/5.0";

        public const string PermitXmlFileName = "PERMIT.XML";

        public const string SchemaFile = @"XmlSchema\Permit_Schema.xsd";

        public const string DigitalSignatureId = "permit";

        public const string PermitSignFileName = "PERMIT.SIGN";
    }
}