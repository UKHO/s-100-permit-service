using FluentAssertions;
using System.Xml;
using System.Xml.Linq;

namespace UKHO.S100PermitService.API.FunctionalTests.Factories
{
    public class PermitXmlFactory : TestBase
    {
        public static void VerifyPermitsZipStructureAndContents(string zipPath, List<string>? invalidChars, List<string> permitHeadersValues, Dictionary<string, string> UPNs, string permitFolderName = "Permits")
        {
            var allFolders = Directory.GetDirectories(zipPath);
            foreach(var folder in allFolders)
            {
                var permitFile = File.Exists(Path.Combine(folder, "PERMIT.XML"));
                permitFile.Should().Be(true);
                var folderName = folder.Split("\\")[folder.Split("\\").Length - 1];
                foreach(var invalidCharacter in invalidChars!)
                {
                    Console.WriteLine(folderName);
                    (folderName.Contains(invalidCharacter)).Should().BeFalse();
                }
                permitHeadersValues[4] = UPNs[folderName];
               
                VerifyPermitHeaderValues(Path.Combine(folder, "PERMIT.XML"), permitHeadersValues);
                VerifyPermitProductValues(Path.Combine(folder, "PERMIT.XML"), Path.Combine($"./TestData/{permitFolderName}/", folderName, "PERMIT.XML")).Should().BeTrue();
                CheckDuplicateFileNameNotPresentInPermitXml(Path.Combine(folder, "PERMIT.XML")).Should().BeFalse();
            }
        }

        public static void VerifyPermitHeaderValues(string path, List<string> permitHeadersValues)
        {
            permitHeadersValues[0] = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            var doc = XDocument.Load(path);
            var allElements = doc.Descendants();
            for(var i = 0 ; i < permitHeadersValues.Count ; i++)
            {
                if(i != 0)
                {
                    allElements.ToArray()!.ElementAt(i + 2).Value.Should().Be(permitHeadersValues[i]);
                }
                allElements.ToArray()!.ElementAt(i + 2).Value.Should().ContainEquivalentOf(permitHeadersValues[i]);             //For issueDate value verifying only the date and igoring Time Zone as this may change
            }
        }

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
                        if(!CompareTestData(generatedXmlDatasetPermit[j], testDataXmlDatasetPermit[j]))
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

        private static bool CompareTestData(XElement generatedXmlDatasetPermit, XElement testDataXmlDatasetPermit)
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

        public static bool CheckDuplicateFileNameNotPresentInPermitXml(string permitFilePath)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(permitFilePath);
            var fileNames = new List<string>();                                                     //For verification of values of fileData
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

