using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class Header
    {
        private string _issueDateField;
        private string _dataServerNameField;
        private string _dataServerIdentifierField;
        private decimal _versionField;
        private string _userpermitField;

        [System.Xml.Serialization.XmlElement("S100SE:issueDate")]
        public string IssueDate
        {
            get
            {
                return _issueDateField;
            }
            set
            {
                _issueDateField = value;
            }
        }

        [System.Xml.Serialization.XmlElement("S100SE:dataServerName")]
        public string DataServerName
        {
            get
            {
                return _dataServerNameField;
            }
            set
            {
                _dataServerNameField = value;
            }
        }

        [System.Xml.Serialization.XmlElement("S100SE:dataServerIdentifier")]
        public string DataServerIdentifier
        {
            get
            {
                return _dataServerIdentifierField;
            }
            set
            {
                _dataServerIdentifierField = value;
            }
        }

        [System.Xml.Serialization.XmlElement("S100SE:version")]
        public decimal Version
        {
            get
            {
                return _versionField;
            }
            set
            {
                _versionField = value;
            }
        }

        [System.Xml.Serialization.XmlElement("S100SE:userpermit")]
        public string Userpermit
        {
            get
            {
                return _userpermitField;
            }
            set
            {
                _userpermitField = value;
            }
        }
    }
}
