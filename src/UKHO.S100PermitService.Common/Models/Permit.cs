using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true, Namespace = "http://www.iho.int/s100/se/5.0")]
    [System.Xml.Serialization.XmlRoot(Namespace = "http://www.iho.int/s100/se/5.0", IsNullable = false)]
    public partial class Permit
    {
        private header headerField;

        private products[] productsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:header")]
        public header header
        {
            get
            {
                return this.headerField;
            }
            set
            {
                this.headerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArray("S100SE:products")]
        [System.Xml.Serialization.XmlArrayItem("S100SE:product", IsNullable = false)]
        public products[] products
        {
            get
            {
                return this.productsField;
            }
            set
            {
                this.productsField = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class header
    {

        private string issueDateField;

        private string dataServerNameField;

        private string dataServerIdentifierField;

        private decimal versionField;

        private string userpermitField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:issueDate")]
        public string issueDate
        {
            get
            {
                return this.issueDateField;
            }
            set
            {
                this.issueDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:dataServerName")]
        public string dataServerName
        {
            get
            {
                return this.dataServerNameField;
            }
            set
            {
                this.dataServerNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:dataServerIdentifier")]
        public string dataServerIdentifier
        {
            get
            {
                return this.dataServerIdentifierField;
            }
            set
            {
                this.dataServerIdentifierField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:version")]
        public decimal version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:userpermit")]
        public string userpermit
        {
            get
            {
                return this.userpermitField;
            }
            set
            {
                this.userpermitField = value;
            }
        }
    }

    /// <remarks/>
    [System.Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class productsProductDatasetPermit
    {

        private string filenameField;

        private byte editionNumberField;

        private string issueDateField;

        private System.DateTime expiryField;

        private string encryptedKeyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:filename")]
        public string filename
        {
            get
            {
                return this.filenameField;
            }
            set
            {
                this.filenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:editionNumber")]
        public byte editionNumber
        {
            get
            {
                return this.editionNumberField;
            }
            set
            {
                this.editionNumberField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:issueDate")]
        public string issueDate
        {
            get
            {
                return this.issueDateField;
            }
            set
            {
                this.issueDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:expiry", DataType = "date")]
        public System.DateTime expiry
        {
            get
            {
                return this.expiryField;
            }
            set
            {
                this.expiryField = value;
            }
        }

        /// <remarks
        [System.Xml.Serialization.XmlElement("S100SE:encryptedKey")]
        public string encryptedKey
        {
            get
            {
                return this.encryptedKeyField;
            }
            set
            {
                this.encryptedKeyField = value;
            }
        }
    }

    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class products
    {
        private productsProductDatasetPermit[] datasetPermitField;

        private string idField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("S100SE:datasetPermit")]
        public productsProductDatasetPermit[] datasetPermit
        {
            get
            {
                return this.datasetPermitField;
            }
            set
            {
                this.datasetPermitField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }
}