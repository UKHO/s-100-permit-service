using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Providers;

namespace UKHO.S100PermitService.Common.Transformer
{
    public class XmlTransformer: IXmlTransformer
    {
        private readonly ISchemaValidator _schemaValidator;
        private readonly ILogger<DigitalSignatureProvider> _logger;
        private readonly string _xsdPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, PermitServiceConstants.SchemaFile);
       
        public XmlTransformer(ILogger<DigitalSignatureProvider> logger, ISchemaValidator schemaValidator)
        {
            _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Serializes the specified object to an XML string with required namespaces and formatting,
         /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object instance to serialize to XML.</param>
        /// <returns>A string containing the serialized and schema-validated XML.</returns>
         public async Task<string> SerializeToXml<T>(T obj)
        {
            _logger.LogInformation(EventIds.XMLSerializationStarted.ToEventId(), "XML serialization process started.");

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

            if(!_schemaValidator.ValidateSchema(xmlContent, _xsdPath))
            {
                throw new PermitServiceException(EventIds.InvalidPermitXmlSchema.ToEventId(), "Invalid xml schema.");
            }

            _logger.LogInformation(EventIds.XMLSerializationCompleted.ToEventId(), "XML serialization process completed.");
            
            return xmlContent;
        }

        /// <summary>
        /// Reads the target namespace from the XSD schema file.
        /// </summary>
        /// <returns>The target namespace string, or null if not found.</returns>
        private string? GetTargetNamespace()
        {
            XmlSchema? schema;
            using (var reader = XmlReader.Create(_xsdPath))
            {
                schema = XmlSchema.Read(reader, null);
            }

            return schema?.TargetNamespace;
        }
    }
}
