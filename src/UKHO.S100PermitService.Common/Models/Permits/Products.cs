using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models.Permits
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class Products
    {
        [System.Xml.Serialization.XmlElement("S100SE:datasetPermit")]
        public List<ProductsProductDatasetPermit> DatasetPermit { get; set; }

        [System.Xml.Serialization.XmlAttribute("id")]
        public string Id { get; set; }
    }
}