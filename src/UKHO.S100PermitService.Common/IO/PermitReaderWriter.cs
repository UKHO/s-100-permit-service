using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UKHO.S100PermitService.Common.Models.Permits;

namespace UKHO.S100PermitService.Common.IO
{
    public class PermitReaderWriter : IPermitReaderWriter
    {
        private const string FirstNamespace = "http://www.iho.int/s100/se/5.1";
        private const string FirstNamespacePrefix = "S100SE";
        private const string SecondNamespace = "http://standards.iso.org/iso/19115/-3/gco/1.0";
        private const string SecondNamespacePrefix = "ns2";
        private const string XmlDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";
        private const string PermitXmlFileName = "PERMIT.XML";

        /// <summary>
        /// Create permit zip
        /// </summary>
        /// <param name="permits"></param>
        /// <returns>ZipStream</returns>
        public MemoryStream CreatePermits(List<Permit> permits)
        {
            var memoryStream = new MemoryStream();
            using(var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach(var permit in permits)
                {
                    CreatePermitXml(archive, $"{permit.Title}/{PermitXmlFileName}", permit);
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
        private void CreatePermitXml(ZipArchive zipArchive, string fileName, Permit permit)
        {
            // Create an entry for the XML file
            var zipEntry = zipArchive.CreateEntry(fileName);

            // Serialize the class to XML
            var serializer = new XmlSerializer(typeof(Permit));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(FirstNamespacePrefix, FirstNamespace);
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
            xmlContent = XmlDeclaration + xmlContent.Replace("_x003A_", ":");

            // Write the modified XML content to the zip entry
            using var streamWriter = new StreamWriter(entryStream);
            streamWriter.Write(xmlContent);
        }
    }
}