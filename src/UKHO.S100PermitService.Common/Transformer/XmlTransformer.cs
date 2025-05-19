using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.IO;

namespace UKHO.S100PermitService.Common.Transformer
{
    public class XmlTransformer(ISchemaValidator schemaValidator) : IXmlTransformer
    {
        private readonly string _xsdPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, PermitServiceConstants.SchemaFile);

        public async Task<string> SerializeToXml<T>(T obj)
        {
            var serializer = new XmlSerializer(typeof(T));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(PermitServiceConstants.FirstNamespacePrefix, GetTargetNamespace() ?? throw new InvalidOperationException("Target namespace cannot be null."));
            namespaces.Add(PermitServiceConstants.SecondNamespacePrefix, PermitServiceConstants.SecondNamespace);

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                Encoding = new UTF8Encoding(false)
            };

            using var memoryStream = new MemoryStream();
            using(var writer = XmlWriter.Create(memoryStream, settings))
            {
                serializer.Serialize(writer, obj, namespaces);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(memoryStream);
            var xmlContent = await reader.ReadToEndAsync();

            xmlContent = PermitServiceConstants.XmlDeclaration + xmlContent.Replace("_x003A_", ":").Replace(PermitServiceConstants.Namespace, GetTargetNamespace() ?? throw new InvalidOperationException("Target namespace cannot be null."));

            if(!schemaValidator.ValidateSchema(xmlContent, _xsdPath))
            {
                throw new PermitServiceException(EventIds.InvalidPermitXmlSchema.ToEventId(), "Invalid xml schema.");
            }
            return xmlContent;
        }

        private string? GetTargetNamespace()
        {
            XmlSchema? schema;
            using(var reader = XmlReader.Create(_xsdPath))
            {
                schema = XmlSchema.Read(reader, null);
            }

            return schema?.TargetNamespace;
        }
    }
}
