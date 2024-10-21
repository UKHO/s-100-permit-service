using FluentAssertions;
using System.Xml;
using System.Xml.Linq;

namespace UKHO.S100PermitService.API.FunctionalTests.Factories
{
    public class PermitXmlFactory
    {
        private static readonly string _permitXml = "PERMIT.XML";

        /// <summary>
        /// This method is used to verify the zip file structure and the PERMIT.XML file contents
        /// </summary>
        /// <param name="zipPath"></param>
        /// <param name="invalidChars"></param>
        /// <param name="permitHeadersValues"></param>
        /// <param name="userPermitNumbers"></param>
        /// <param name="permitFolderName"></param>
        public static void VerifyPermitsZipStructureAndPermitXmlContents(string zipPath, List<string>? invalidChars, List<string> permitHeadersValues, IReadOnlyDictionary<string, string> userPermitNumbers, string permitFolderName = "Permits")
        {
            var allFolders = Directory.GetDirectories(zipPath);
            foreach(var folder in allFolders)
            {
                var folderName = Path.GetFileName(folder);
                foreach(var invalidCharacter in invalidChars!)
                {
                    (folderName.Contains(invalidCharacter)).Should().BeFalse();
                }
                var permitFile = File.Exists(Path.Combine(folder, _permitXml));
                permitFile.Should().Be(true);
                permitHeadersValues[4] = userPermitNumbers[folderName];

                VerifyPermitHeaderValues(Path.Combine(folder, _permitXml), permitHeadersValues);
                VerifyPermitProductValues(Path.Combine(folder, _permitXml), Path.Combine($"./TestData/{permitFolderName}/", folderName, _permitXml)).Should().BeTrue();
                VerifyDuplicateFileNameNotPresentInPermitXml(Path.Combine(folder, _permitXml)).Should().BeFalse();
            }
        }

        /// <summary>
        /// This method is used to verify the values of header tag in PERMIT.XML
        /// </summary>
        /// <param name="path"></param>
        /// <param name="permitHeadersValues"></param>
        public static void VerifyPermitHeaderValues(string path, List<string> permitHeadersValues)
        {
            permitHeadersValues[0] = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            var doc = XDocument.Load(path);
            var allElements = doc.Descendants();
            for(var i = 0 ; i < permitHeadersValues.Count ; i++)
            {
                if(i != 0)
                {
                    allElements.ToArray().ElementAt(i + 2).Value.Should().Be(permitHeadersValues[i]);
                }
                allElements.ToArray().ElementAt(i + 2).Value.Should().ContainEquivalentOf(permitHeadersValues[i]);
            }
        }

        /// <summary>
        /// This method is used to verify the values of product tags in PERMIT.XML
        /// </summary>
        /// <param name="generatedXmlFilePath"></param>
        /// <param name="xmlFilePath"></param>
        /// <returns></returns>
        public static bool VerifyPermitProductValues(string generatedXmlFilePath, string xmlFilePath)
        {
            var generatedXml = XElement.Load(generatedXmlFilePath);
            var testDataXml = XElement.Load(xmlFilePath);

            if(generatedXml.Descendants().Count() == testDataXml.Descendants().Count())
            {
                var generatedXmlProducts = generatedXml.Elements(XName.Get("products", "http://www.iho.int/s100/se/5.2")).ToList();

                var testDataXmlProducts = testDataXml.Elements(XName.Get("products", "http://www.iho.int/s100/se/5.2")).ToList();

                for(var i = 0 ; i < generatedXmlProducts.Count ; i++)
                {
                    var generatedXmlProduct = generatedXmlProducts[i];
                    var testDataXmlProduct = testDataXmlProducts[i];

                    var generatedXmlDatasetPermit = generatedXmlProduct.Descendants(XName.Get("datasetPermit", "http://www.iho.int/s100/se/5.2")).ToList();
                    var testDataXmlDatasetPermit = testDataXmlProduct.Descendants(XName.Get("datasetPermit", "http://www.iho.int/s100/se/5.2")).ToList();

                    // Compare each testData
                    for(var j = 0 ; j < generatedXmlDatasetPermit.Count ; j++)
                    {
                        if(!ComparePermitXmlData(generatedXmlDatasetPermit[j], testDataXmlDatasetPermit[j]))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// This method is used to compare PERMIT.XML data
        /// </summary>
        /// <param name="generatedXmlDatasetPermit"></param>
        /// <param name="testDataXmlDatasetPermit"></param>
        /// <returns></returns>
        private static bool ComparePermitXmlData(XElement generatedXmlDatasetPermit, XElement testDataXmlDatasetPermit)
        {
            // Compare relevant elements in testData
            var elementsToCompare = new[] { "filename", "editionNumber", "expiry", "encryptedKey" };

            foreach(var elementName in elementsToCompare)
            {
                var generatedXmlElement = generatedXmlDatasetPermit.Element(XName.Get(elementName, "http://www.iho.int/s100/se/5.2"))?.Value;
                var testDataXmlElement = testDataXmlDatasetPermit.Element(XName.Get(elementName, "http://www.iho.int/s100/se/5.2"))?.Value;
                if(generatedXmlElement != testDataXmlElement)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// This method is used to verify duplicate file name is not present in PERMIT.XML
        /// </summary>
        /// <param name="permitFilePath"></param>
        /// <returns></returns>
        public static bool VerifyDuplicateFileNameNotPresentInPermitXml(string permitFilePath)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(permitFilePath);
            var fileNames = new List<string>();
            var checkDuplicate = new HashSet<string>();
            var hasDuplicates = false;
            var nodes = xmlDoc.GetElementsByTagName("S100SE:datasetPermit");
            foreach(XmlNode node in nodes)
            {
                var fileName = node["S100SE:filename"]!.InnerText;
                fileNames.Add(fileName);
            }

            foreach(var fileName in fileNames.ToArray())
            {
                if(!checkDuplicate.Add(fileName))
                {
                    hasDuplicates = true;
                }
            }
            return hasDuplicates;
        }

        /// <summary>
        /// This method is used to delete the folder.
        /// </summary>
        /// <param name="folderPath"></param>
        public static void DeleteFolder(string folderPath)
        {
            if(Directory.Exists(folderPath))
            {
                // Delete the folder and its contents
                Directory.Delete(folderPath, true);
            }
        }
    }
}