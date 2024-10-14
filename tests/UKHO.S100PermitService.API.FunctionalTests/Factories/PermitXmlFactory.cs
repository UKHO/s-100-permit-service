using FluentAssertions;
using System.IO.Compression;
using System.Xml;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;

namespace UKHO.S100PermitService.API.FunctionalTests.Factories
{
    public class PermitXmlFactory : TestBase
    {
        private PermitServiceApiConfiguration? _permitServiceApiConfiguration;

        public static string[] ExtractZipAndGetAllFolders(string zipPath,string extractPath)
        {
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            var allFolders = Directory.GetDirectories(extractPath);

            return allFolders;
        }

        public static void CheckZipStructureAndContent(string zipPath, string extractPath, List<string>? InvalidChars)
        {
            var allFolders = ExtractZipAndGetAllFolders(@"D:\Work\Aasvhith\Permit Service\Docs\Permits.zip", @"D:\Work\Aasvhith\Permit Service\Docs\Permits");
            foreach(var folder in allFolders)
            {
                var folderName = folder.Split("\\")[folder.Split("\\").Length - 1];
                foreach(var invalidCharacter in InvalidChars)
                {
                    (folderName.Contains(invalidCharacter)).Should().Be(false);
                }
                var permitFile = File.Exists(Path.Combine(folder, "PERMIT.XML"));
                permitFile.Should().Be(true);

            }
        }

        public static XmlDocument ReadXmlFile(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@"D:\Work\Aasvhith\Permit Service\Docs\PERMIT.XML");

            return xmlDoc;
        }

        public static string ReadSingleXmlNodeValue(string xPath)
        {
            var xmlDoc = ReadXmlFile(@"D:\Work\Aasvhith\Permit Service\Docs\PERMIT.XML");
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);                           //For verification of values in header
            nsmgr.AddNamespace("S100SE", "http://www.iho.int/s100/se/5.1");
            var headerValue = xmlDoc.SelectSingleNode($"//S100SE:{xPath}", nsmgr)!.InnerText;
            return headerValue;
        }

        public static bool CheckDuplicateFileNameNotPresentInPermitXml(string permitFilePath)
        {
            var xmlDoc = ReadXmlFile(@"D:\Work\Aasvhith\Permit Service\Docs\PERMIT.XML");
            List<string> fileNames = new List<string>();                                                     //For verification of values of fileData
            HashSet<string> checkDuplicate = new HashSet<string>();
            bool hasDuplicates = false;
            XmlNodeList nodes = xmlDoc.GetElementsByTagName("S100SE:datasetPermit");
            foreach(XmlNode node in nodes)
            {
                var fileName = node["S100SE:filename"].InnerText;
                fileNames.Add(fileName);
                ////var edition = node["S100SE:editionNumber"].InnerText;
                ////var issueDate = node["S100SE:issueDate"].InnerText;
                ////var expiry = node["S100SE:expiry"].InnerText;
                ////var encryptedKey = node["S100SE:encryptedKey"].InnerText;
            }

            string[] fileNamesArray = fileNames.ToArray();

            foreach(var fileName in fileNamesArray)
            {

                if(!checkDuplicate.Add(fileName))
                {
                    hasDuplicates = true;
                }
            }
            //hasDuplicates.Should().BeFalse();

            return hasDuplicates;
        }
    }
}
