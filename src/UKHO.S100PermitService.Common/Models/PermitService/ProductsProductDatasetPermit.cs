using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models.PermitService
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ProductsProductDatasetPermit
    {
        private string _fileNameField;
        private byte _editionNumberField;
        private string _issueDateField;
        private DateTime _expiryField;
        private string _encryptedKeyField;

        [System.Xml.Serialization.XmlElement("S100SE:filename")]
        public string Filename
        {
            get
            {
                return _fileNameField;
            }
            set
            {
                _fileNameField = value;
            }
        }

        [System.Xml.Serialization.XmlElement("S100SE:editionNumber")]
        public byte EditionNumber
        {
            get
            {
                return _editionNumberField;
            }
            set
            {
                _editionNumberField = value;
            }
        }

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

        [System.Xml.Serialization.XmlElement("S100SE:expiry", DataType = "date")]
        public DateTime Expiry
        {
            get
            {
                return _expiryField;
            }
            set
            {
                _expiryField = value;
            }
        }

        [System.Xml.Serialization.XmlElement("S100SE:encryptedKey")]
        public string EncryptedKey
        {
            get
            {
                return _encryptedKeyField;
            }
            set
            {
                _encryptedKeyField = value;
            }
        }
    }
}