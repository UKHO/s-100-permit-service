using Microsoft.Extensions.Logging;
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
        private const string PermitXmlFileName = "PERMIT.XML";
        private const string SchemaFile = @"XmlSchema\Permit_Schema.xsd";
        private readonly string _xsdPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, SchemaFile);

        private readonly ILogger<PermitReaderWriter> _logger;
        private readonly ISchemaValidator _schemaValidator;
       

        public PermitReaderWriter(ILogger<PermitReaderWriter> logger, 
                                  ISchemaValidator schemaValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        }

        /// <summary>
        /// Read Xsd version from schema file
        /// </summary>
        /// <returns></returns>
        public string ReadXsdVersion()
        {
            XmlSchema? schema;
            using(var reader = XmlReader.Create(_xsdPath))
            {
                schema = XmlSchema.Read(reader, null);
            }

            return schema?.Version[..5] ?? null;
        }

        /// <summary>
        /// Create permit zip
        /// </summary>
        /// <param name="permits"></param>
        /// <returns>ZipStream</returns>
        public async Task<Stream> CreatePermitZip(IReadOnlyDictionary<string, Permit> permits)
        {
            var memoryStream = new MemoryStream();
            using(var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach(var permit in permits)
                {
                  await CreatePermitXml(archive, permit.Key, permit.Value);
                }
            }

            _logger.LogInformation(EventIds.PermitZipFileCreationCompleted.ToEventId(), "Permit zip file creation completed.");

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        /// <summary>
        /// Create permit xml files and add into permit zip 
        /// </summary>
        /// <param name="zipArchive"></param>
        /// <param name="upnTitle"></param>
        /// <param name="permit"></param>
        private async Task CreatePermitXml(ZipArchive zipArchive, string upnTitle, Permit permit)
        {
            _logger.LogInformation(EventIds.PermitXmlFileCreationStarted.ToEventId(), "Creation of Permit XML file for UPN: {UpnTitle} started.", upnTitle);
            
            var fileName= $"{upnTitle}/{PermitXmlFileName}";
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

            // Validate schema
            if(!_schemaValidator.ValidateSchema(xmlContent, _xsdPath))
            {
                throw new PermitServiceException(EventIds.InvalidPermitXmlSchema.ToEventId(), "Invalid permit xml schema.");
            }

            // Write the modified XML content to the zip entry
            using var streamWriter = new StreamWriter(entryStream);
            await streamWriter.WriteAsync(xmlContent);

            _logger.LogInformation(EventIds.PermitXmlFileCreationCompleted.ToEventId(), "Creation of Permit XML file for UPN {UpnTitle} completed.", upnTitle);
        }

        private string GetTargetNamespace()
        {
            XmlSchema? schema;
            using(var reader = XmlReader.Create(_xsdPath))
            {
                schema = XmlSchema.Read(reader, null);
            }

            return schema?.TargetNamespace ?? null;
        }
    }
}