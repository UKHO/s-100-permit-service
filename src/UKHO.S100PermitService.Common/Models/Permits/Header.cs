using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models.Permits
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class Header
    {
        [System.Xml.Serialization.XmlElement("S100SE:issueDate")]
        public string IssueDate { get; set; }

        [System.Xml.Serialization.XmlElement("S100SE:dataServerName")]
        public string DataServerName { get; set; }

        [System.Xml.Serialization.XmlElement("S100SE:dataServerIdentifier")]
        public string DataServerIdentifier { get; set; }

        [System.Xml.Serialization.XmlElement("S100SE:version")]
        public string Version { get; set; }

        [System.Xml.Serialization.XmlElement("S100SE:userpermit")]
        public string Userpermit { get; set; }
    }
}