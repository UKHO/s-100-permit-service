using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.Permits;

namespace UKHO.S100PermitService.Common.IO
{
    public class PermitReaderWriter : IPermitReaderWriter
    {
        private const string FirstNamespacePrefix = "S100SE";
        private const string SecondNamespace = "http://standards.iso.org/iso/19115/-3/gco/1.0";
        private const string SecondNamespacePrefix = "ns2";
        private const string XmlDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";
        private const string Namespace = "http://www.iho.int/s100/se/5.0";
        private const string SchemaFolder = "XmlSchema";
        private const string PermitSchema = "Permit_Schema.xsd";
        private const string PermitXmlFileName = "PERMIT.XML";
        private const string SchemaFile = @"XmlSchema\Permit_Schema.xsd";
        private readonly string _schemaDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        /// <summary>
        /// Create permit zip
        /// </summary>
        /// <param name="permits"></param>
        /// <returns>ZipStream</returns>
        public MemoryStream CreatePermits(Dictionary<string, Permit> permits)
        {
            var xsdPath = Path.Combine(_schemaDirectory, SchemaFile);

            var memoryStream = new MemoryStream();
            using(var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach(var permit in permits)
                {
                    CreatePermitXml(archive, $"{permit.Key}/{PermitXmlFileName}", permit.Value, xsdPath);
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        /// <summary>
        /// Create permit xml files and add into permit zip 
        /// </summary>
        /// <param name="zipArchive"></param>
        /// <param name="fileName"></param>
        /// <param name="permit"></param>
        private void CreatePermitXml(ZipArchive zipArchive, string fileName, Permit permit, string xsdPath)
        {
            // Create an entry for the XML file
            var zipEntry = zipArchive.CreateEntry(fileName);

            // Serialize the class to XML
            var serializer = new XmlSerializer(typeof(Permit));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(FirstNamespacePrefix, GetTargetNamespace());
            namespaces.Add(SecondNamespacePrefix, SecondNamespace);

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                Encoding = new UTF8Encoding(false)
            };

            using var entryStream = zipEntry.Open();
            using var memoryStream = new MemoryStream();
            using(var writer = XmlWriter.Create(memoryStream, settings))
            {
                serializer.Serialize(writer, permit, namespaces);
            }

            // Reset the position of the MemoryStream to the beginning
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Read the XML content from the MemoryStream
            using var reader = new StreamReader(memoryStream);
            var xmlContent = reader.ReadToEnd();

            // Replace "_x003A_" with ":"
            xmlContent = XmlDeclaration + xmlContent.Replace("_x003A_", ":").Replace(Namespace, GetTargetNamespace());

            if(!ValidateSchema(xmlContent, xsdPath))
            {
                throw new PermitServiceException(EventIds.InvalidPermitXmlSchema.ToEventId(), "Invalid xml schema is received");
            }

            // Write the modified XML content to the zip entry
            using var streamWriter = new StreamWriter(entryStream);
            streamWriter.Write(xmlContent);
        }

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

        private string GetTargetNamespace()
        {
            var xsdPath = Path.Combine(_schemaDirectory, SchemaFolder, PermitSchema);

            XmlSchema? schema;
            using(var reader = XmlReader.Create(xsdPath))
            {
                schema = XmlSchema.Read(reader, null);
            }

            return schema?.TargetNamespace ?? null;
        }
    }
}