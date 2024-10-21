using System.Xml;
using System.Xml.Schema;

namespace UKHO.S100PermitService.Common.IO
{
    public class SchemaValidator : ISchemaValidator
    {
        public bool ValidateSchema(string permitXml, string xsdPath)
        {
            var xml = new XmlDocument();
            xml.LoadXml(permitXml);

            var xmlSchemaSet = new XmlSchemaSet();
            xmlSchemaSet.Add(null, xsdPath);

            xml.Schemas = xmlSchemaSet;

            var validXml = true;
            try
            {
                xml.Validate((sender, e) =>
                {
                    validXml = false;
                });
            }
            catch(XmlSchemaValidationException)
            {
                validXml = false;
                return validXml;
            }
            return validXml;
        }
    }
}