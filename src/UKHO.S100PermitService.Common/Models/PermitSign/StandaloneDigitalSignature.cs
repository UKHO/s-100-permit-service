using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.S100PermitService.Common.Models.PermitSign
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.iho.int/s100/se/5.0")]
    [XmlRoot("S100SE:StandaloneDigitalSignature", Namespace = "http://www.iho.int/s100/se/5.0", IsNullable = false)]
    public class StandaloneDigitalSignature
    {
        [XmlElement("S100SE:filename")]
        public string Filename { get; set; }

        [XmlElement("S100SE:certificates")]
        public Certificate Certificate { get; set; }

        [XmlElement("S100SE:digitalSignature")]
        public DigitalSignatureInfo DigitalSignatureInfo { get; set; }
    }
}
