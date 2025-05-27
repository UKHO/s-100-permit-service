using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.S100PermitService.Common.Models.PermitSign
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public class SchemeAdministrator
    {
        [XmlAttribute("id")]
        public string Id { get; set; }
    }
}
