using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.S100PermitService.Common.Models.PermitSign
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public class DigitalSignature
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("certificateRef")]
        public string CertificateRef { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
