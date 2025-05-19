using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.S100PermitService.Common.Models.PermitSign
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public class Certificate
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("issuer")]
        public string Issuer { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
