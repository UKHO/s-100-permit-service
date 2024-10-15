using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
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

        private readonly string _schemaDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        private readonly IFileSystem _fileSystem;

        public PermitReaderWriter(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public string ReadPermit(Permit permit)
        {
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

            using var stringWriter = new StringWriter();
            using var writer = XmlWriter.Create(stringWriter, settings);
            serializer.Serialize(writer, permit, namespaces);
            var xml = stringWriter.ToString().Replace("_x003A_", ":");
            xml = xml.Replace(Namespace, GetTargetNamespace());
            return (XmlDeclaration + xml);
        }

        [ExcludeFromCodeCoverage]
        public void WritePermit(string fileContent)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "Master", $"PERMIT.xml");
                var directoryPath = Path.GetDirectoryName(tempPath);
                if(!_fileSystem.Directory.Exists(directoryPath))
                {
                    _fileSystem.Directory.CreateDirectory(directoryPath);
                }
                var fileStream = new FileStream(tempPath, FileMode.OpenOrCreate);
                if(_fileSystem.File.Exists(tempPath))
                {
                    fileStream.Close();
                    _fileSystem.File.WriteAllText(tempPath, fileContent);
                }
            }
            catch(Exception)
            {
                throw;
            }
        }

        private string GetTargetNamespace()
        {
            var xsdPath = Path.Combine(_schemaDirectory, "XmlSchema", "Permit_Schema.xsd");

            XmlSchema? schema;
            using(var reader = XmlReader.Create(xsdPath))
            {
                schema = XmlSchema.Read(reader, null);
            }

            return schema?.TargetNamespace ?? null;
        }
    }
}