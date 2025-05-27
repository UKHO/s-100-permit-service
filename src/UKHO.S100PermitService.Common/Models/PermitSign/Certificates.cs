using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.S100PermitService.Common.Models.PermitSign
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public class Certificates
    {
        [XmlElement("S100SE:schemeAdministrator")]
        public SchemeAdministrator SchemeAdministrator { get; set; }

        [XmlElement("S100SE:certificate")]
        public CertificateMetadata CertificateMetadata { get; set; }
    }
}
