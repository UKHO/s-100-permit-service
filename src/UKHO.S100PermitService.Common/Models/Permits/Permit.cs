using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Models.Permits
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true, Namespace = "http://www.iho.int/s100/se/5.0")]
    [System.Xml.Serialization.XmlRoot(Namespace = "http://www.iho.int/s100/se/5.0", IsNullable = false)]
    public partial class Permit
    {
        private Header _headerField;
        private Products[] _productsField;

        [System.Xml.Serialization.XmlElement("S100SE:header")]
        public Header Header
        {
            get
            {
                return _headerField;
            }
            set
            {
                _headerField = value;
            }
        }

        [System.Xml.Serialization.XmlArray("S100SE:products")]
        [System.Xml.Serialization.XmlArrayItem("S100SE:product", IsNullable = false)]
        public Products[] Products
        {
            get
            {
                return _productsField;
            }
            set
            {
                _productsField = value;
            }
        }
    }
}