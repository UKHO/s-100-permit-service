using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.S100PermitService.Common.Models.Permits
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    [ExcludeFromCodeCoverage]
    [Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.iho.int/s100/se/5.0")]
    [XmlRoot(Namespace = "http://www.iho.int/s100/se/5.0", IsNullable = false)]
    public partial class Permit
    {
        private Header _headerField;
        private Products[] _productsField;

        [XmlElement("S100SE:header")]
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

        [XmlArray("S100SE:products")]
        [XmlArrayItem("S100SE:product", IsNullable = false)]
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