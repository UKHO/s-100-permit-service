using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Transformers;

namespace UKHO.S100PermitService.Common.IO
{
    public class PermitReaderWriter : IPermitReaderWriter
    {
        private readonly string _xsdPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, PermitServiceConstants.SchemaFile);

        private readonly ILogger<PermitReaderWriter> _logger;
        private readonly IPermitSignGeneratorService _permitSignGeneratorService;
        private readonly IXmlTransformer _xmlTransformer;

        public PermitReaderWriter(ILogger<PermitReaderWriter> logger, IPermitSignGeneratorService permitSignGeneratorService, IXmlTransformer xmlTransformer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    var permitXmlContent = await CreatePermitXmlAndAddToZipAsync(archive, permit.Key, permit.Value);
                    try
                    {
                        await CreatePermitSignAndAddToZipAsync(archive, permit.Key, permitXmlContent);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
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
        private async Task<string> CreatePermitXmlAndAddToZipAsync(ZipArchive zipArchive, string upnTitle, Permit permit)
        {
            _logger.LogInformation(EventIds.PermitXmlCreationStarted.ToEventId(), "Creation of Permit XML for UPN: {UpnTitle} started.", upnTitle);

            var fileName = GetFileName(upnTitle, PermitServiceConstants.PermitXmlFileName);

            var permitXmlContent = await _xmlTransformer.SerializeToXml(permit);

            await AddEntryToZipAsync(zipArchive, fileName, permitXmlContent);

            _logger.LogInformation(EventIds.PermitXmlCreationCompleted.ToEventId(), "Creation of Permit XML for UPN: {UpnTitle} completed.", upnTitle);

            return permitXmlContent;
        }

        /// <summary>
        /// Generates the permit signature XML for the specified permit XML content and adds it to the provided zip archive.
        /// </summary>
        /// <param name="zipArchive">The zip archive to add the permit signature to.</param>
        /// <param name="upnTitle">The user permit title used for naming the signature file.</param>
        /// <param name="permitXmlContent">The XML content of the permit to be signed.</param>
        private async Task CreatePermitSignAndAddToZipAsync(ZipArchive zipArchive, string upnTitle, string permitXmlContent)
        {
            _logger.LogInformation(EventIds.PermitSignCreationStarted.ToEventId(), "Creation of Permit SIGN for UPN: {UpnTitle} started.", upnTitle);

            var fileName = GetFileName(upnTitle, PermitServiceConstants.PermitSignFileName);

            var signXmlContent = await _permitSignGeneratorService.GeneratePermitSignXmlAsync(permitXmlContent);

            await AddEntryToZipAsync(zipArchive, fileName, signXmlContent);

            _logger.LogInformation(EventIds.PermitSignCreationCompleted.ToEventId(), "Creation of Permit SIGN for UPN: {UpnTitle} completed.", upnTitle);
        }

        /// <summary>
        /// Generates a formatted file name for a zip entry using the user permit title and the specified file name.
        /// </summary>
        /// <param name="upnTitle">User permit title.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>Formatted file name.</returns>
        private static string GetFileName(string upnTitle, string fileName)
        {
            return $"{upnTitle}/{fileName}";
        }

        /// <summary>
        /// Adds the specified content as a new entry to the provided zip archive.
        /// </summary>
        /// <param name="zipArchive">Zip object.</param>
        /// <param name="fileName">File name for the entry.</param>
        /// <param name="content">Content to write.</param>
        private static async Task AddEntryToZipAsync(ZipArchive zipArchive, string fileName, string content)
        {
            var zipEntry = zipArchive.CreateEntry(fileName);

            await using var entryStream = zipEntry.Open();
            await using var streamWriter = new StreamWriter(entryStream);
            await streamWriter.WriteAsync(content);
        }
    }
}