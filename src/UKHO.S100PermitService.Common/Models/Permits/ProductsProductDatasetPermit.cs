using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models.Permits
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ProductsProductDatasetPermit
    {
        [System.Xml.Serialization.XmlElement("S100SE:filename")]
        public string Filename { get; set; }

        [System.Xml.Serialization.XmlElement("S100SE:editionNumber")]
        public byte EditionNumber { get; set; }

        [System.Xml.Serialization.XmlElement("S100SE:expiry")]
        public string Expiry { get; set; }
        
        [System.Xml.Serialization.XmlElement("S100SE:encryptedKey")]
        public string EncryptedKey { get; set; }
    }
}