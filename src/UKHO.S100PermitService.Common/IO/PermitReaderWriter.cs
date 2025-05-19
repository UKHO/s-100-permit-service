using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Transformer;

namespace UKHO.S100PermitService.Common.IO
{
    public class PermitReaderWriter : IPermitReaderWriter
    {
        private readonly string _xsdPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, PermitServiceConstants.SchemaFile);

        private readonly ILogger<PermitReaderWriter> _logger;
        private readonly ISchemaValidator _schemaValidator;
        private readonly IPermitSignGeneratorService _permitSignGeneratorService;
        private readonly IXmlTransformer _xmlTransformer;

        public PermitReaderWriter(ILogger<PermitReaderWriter> logger,
                                  ISchemaValidator schemaValidator, IPermitSignGeneratorService permitSignGeneratorService, IXmlTransformer xmlTransformer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
            _permitSignGeneratorService = permitSignGeneratorService ?? throw new ArgumentNullException(nameof(permitSignGeneratorService));
            _xmlTransformer = xmlTransformer ?? throw new ArgumentNullException(nameof(xmlTransformer));
        }

        /// <summary>
        /// Read Xsd version from schema file
        /// </summary>
        /// <returns>Xsd version</returns>
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
        /// <param name="permits">Permit details.</param>
        /// <returns>Zip Stream</returns>
        public async Task<Stream> CreatePermitZipAsync(IReadOnlyDictionary<string, Permit> permits)
        {
            var memoryStream = new MemoryStream();
            using(var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach(var permit in permits)
                {
                    var permitXmlContent = await CreatePermitXmlAsync(archive, permit.Key, permit.Value);
                    await CreatePermitSignAsync(permit.Key, permitXmlContent);
                }
            }

            _logger.LogInformation(EventIds.PermitZipCreationCompleted.ToEventId(), "Permit zip creation completed.");

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        /// <summary>
        /// Create permit xml and add into permit zip 
        /// </summary>
        /// <param name="zipArchive">Zip object.</param>
        /// <param name="upnTitle">User permit title.</param>
        /// <param name="permit">Permit details.</param>
        private async Task<string> CreatePermitXmlAsync(ZipArchive zipArchive, string upnTitle, Permit permit)
        {
            _logger.LogInformation(EventIds.PermitXmlCreationStarted.ToEventId(), "Creation of Permit XML for UPN: {UpnTitle} started.", upnTitle);

            var fileName = $"{upnTitle}/{PermitServiceConstants.PermitXmlFileName}";
            // Create an entry for the XML file
            var zipEntry = zipArchive.CreateEntry(fileName);

            var permitXmlContent = await _xmlTransformer.SerializeToXml(permit);
            
            _logger.LogInformation(EventIds.PermitXmlCreationCompleted.ToEventId(), "Creation of Permit XML for UPN: {UpnTitle} completed.", upnTitle);

            return permitXmlContent;
        }

        /// <summary>
        /// Create permit sign and add into permit zip
        /// </summary>
        /// <param name="upnTitle">User permit title.</param>
        /// <param name="permitXmlContent"></param>
        private async Task CreatePermitSignAsync(string upnTitle, string permitXmlContent)
        {
            _logger.LogInformation(EventIds.PermitSignCreationStarted.ToEventId(), "Creation of Permit SIGN for UPN: {UpnTitle} started.", upnTitle);
            var signXmlContent = await _permitSignGeneratorService.GeneratePermitSignXmlAsync(permitXmlContent);
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