using System.Text.RegularExpressions;

namespace UKHO.S100PermitService.Common.Extensions
{
    public static class CertificateExtension
    {
        /// <summary>
        /// Extracts the Common Name (CN) value from a certificate subject or issuer string.
        /// </summary>
        /// <param name="content">The subject or issuer string from an X509 certificate (e.g., "CN=Example, O=Org, C=GB").</param>
        /// <returns>
        /// The value of the CN field if present; otherwise, returns "UnknownCN".
        /// </returns>
        public static string GetCnFromCertificate(this string content)
        {
            var match = Regex.Match(content, @"CN=([^,]+)");
            return match.Success ? match.Groups[1].Value : "UnknownCN";
        }
    }
}
