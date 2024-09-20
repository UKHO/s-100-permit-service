using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models.Permits
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class Products
    {
        private ProductsProductDatasetPermit[] _datasetPermitField;
        private string _idField;

        [System.Xml.Serialization.XmlElement("S100SE:datasetPermit")]
        public ProductsProductDatasetPermit[] DatasetPermit
        {
            get
            {
                return _datasetPermitField;
            }
            set
            {
                _datasetPermitField = value;
            }
        }

        [System.Xml.Serialization.XmlAttribute("id")]
        public string Id
        {
            get
            {
                return _idField;
            }
            set
            {
                _idField = value;
            }
        }
    }
}