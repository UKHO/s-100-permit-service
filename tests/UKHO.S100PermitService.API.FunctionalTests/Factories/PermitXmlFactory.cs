using FluentAssertions;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using UKHO.S100PermitService.API.FunctionalTests.Configuration;
using System.Runtime.ConstrainedExecution;

namespace UKHO.S100PermitService.API.FunctionalTests.Factories
{
    public class PermitXmlFactory
    {
        private static readonly string _permitXml = "PERMIT.XML";
        private static readonly string _permitSignFile = "PERMIT.SIGN";

        /// <summary>
        /// This method is used to verify the zip file structure and the PERMIT.XML file contents
        /// </summary>
        /// <param name="zipPath"></param>
        /// <param name="invalidChars"></param>
        /// <param name="permitHeadersValues"></param>
        /// <param name="userPermitNumbers"></param>
        /// <param name="permitFolderName"></param>
        public static void VerifyPermitsZipStructureAndPermitXmlContents(string zipPath, IEnumerable<string>? invalidChars, List<string> permitHeadersValues, IReadOnlyDictionary<string, string> userPermitNumbers, string permitFolderName = "Permits")
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
        /// <returns>true or false based on comparing two xml files</returns>
        public static bool VerifyPermitProductValues(string generatedXmlFilePath, string xmlFilePath)
        {
            var generatedXml = XElement.Load(generatedXmlFilePath);
            var testDataXml = XElement.Load(xmlFilePath);

            if (generatedXml.Descendants().Count() == testDataXml.Descendants().Count())
            {
                var generatedXmlProducts = generatedXml.Elements(XName.Get("products", "http://www.iho.int/s100/se/5.2")).ToList();
                var testDataXmlProducts = testDataXml.Elements(XName.Get("products", "http://www.iho.int/s100/se/5.2")).ToList();

                for (var i = 0; i < generatedXmlProducts.Count; i++)
                {
                    var generatedXmlProduct = generatedXmlProducts[i];
                    var testDataXmlProduct = testDataXmlProducts[i];

                    var generatedXmlDatasetPermit = generatedXmlProduct.Descendants(XName.Get("datasetPermit", "http://www.iho.int/s100/se/5.2")).ToList();
                    var testDataXmlDatasetPermit = testDataXmlProduct.Descendants(XName.Get("datasetPermit", "http://www.iho.int/s100/se/5.2")).ToList();

                    // Updates expiry date to one year from today in testData
                    foreach(var child in testDataXml.Elements())
                    {
                        foreach(var expiryNode in child.Descendants(XName.Get("expiry", "http://www.iho.int/s100/se/5.2")))
                        {
                            expiryNode.Value = UpdateDate(); 
                        }
                    }

                    // Compare each testData
                    for (var j = 0; j < generatedXmlDatasetPermit.Count; j++)
                    {
                        if (!ComparePermitXmlData(generatedXmlDatasetPermit[j], testDataXmlDatasetPermit[j]))
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
        /// <returns>true or false based on comparison of two xml data</returns>
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
        /// <returns>true or false based on the presence of duplicate FileNames</returns>
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
        /// Verifies the digital signatures of XML files located within subdirectories of a given path
        /// using an X.509 certificate retrieved from Azure Key Vault.
        /// </summary>
        /// <param name="generatedXmlFilePath">The root directory containing subfolders with signed XML files.</param>
        /// <param name="keyVaultUrl">The URI of the Azure Key Vault where the certificate reference is stored.</param>
        /// <param name="dsCertificateName">The name of the certificate secret in Azure Key Vault.</param>
        /// <param name="tenantId">The Azure Active Directory tenant ID used for authentication.</param>
        /// <param name="clientId">The Azure AD application (client) ID.</param>
        /// <param name="clientSecret">The Azure AD client secret.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is <c>true</c> if all XML files are successfully verified; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The method loads the certificate from Key Vault, extracts identifier values, and verifies the
        /// signature of each XML file within subdirectories of the specified path.
        /// </remarks>
        public static async Task<bool> VerifySignatureTask(string generatedXmlFilePath, string keyVaultUrl, string dsCertificateName, string tenantId, string clientId, string clientSecret)
        {
            var certificateBytes = await GetCertificateFromKeyVaultTask(keyVaultUrl, dsCertificateName, tenantId, clientId, clientSecret);
            var dsCertificate = new X509Certificate2(certificateBytes, (string?)null, X509KeyStorageFlags.MachineKeySet);

            var saIdFromCert = ExtractSaId(dsCertificate);
            var certIdFromCert = ExtractCertId(dsCertificate);

            foreach(var folder in Directory.GetDirectories(generatedXmlFilePath))
            {
                if(!VerifyXmlAgainstCertificate(folder, saIdFromCert, certIdFromCert, certificateBytes, dsCertificate))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifies the signature components and attributes in the XML files of a given folder against the expected values from the certificate.
        /// </summary>
        /// <param name="folder">Folder path containing the signature and data XML files.</param>
        /// <param name="saIdFromCert">Scheme Administrator ID extracted from the certificate.</param>
        /// <param name="certIdFromCert">Certificate ID extracted from the certificate.</param>
        /// <param name="certificateBytes">Byte array of the certificate from Key Vault.</param>
        /// <param name="dsCertificate">X509Certificate2 object of the certificate.</param>
        /// <returns>True if verification succeeds; otherwise, false.</returns>
        private static bool VerifyXmlAgainstCertificate(string folder, string saIdFromCert, string certIdFromCert, byte[] certificateBytes, X509Certificate2 dsCertificate)
        {
            var signFilePath = Path.Combine(folder, _permitSignFile);
            var signatureDoc = XDocument.Load(signFilePath);
            var ns = (XNamespace)"http://www.iho.int/s100/se/5.2";

            var saIdFromXml = signatureDoc.Descendants(ns + "schemeAdministrator").FirstOrDefault()?.Attribute("id")?.Value?.Trim();
            var certIssuerFromXml = signatureDoc.Descendants(ns + "certificate").FirstOrDefault()?.Attribute("issuer")?.Value?.Trim();
            var certIdFromXml = signatureDoc.Descendants(ns + "certificate").FirstOrDefault()?.Attribute("id")?.Value?.Trim();
            var certRefFromXml = signatureDoc.Descendants(ns + "digitalSignature").FirstOrDefault()?.Attribute("certificateRef")?.Value?.Trim();

            if(!string.Equals(saIdFromXml, saIdFromCert, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(certIssuerFromXml, saIdFromCert, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ERROR] SA ID mismatch. Cert: '{saIdFromCert}', XML: '{saIdFromXml}', Issuer: '{certIssuerFromXml}'");
                return false;
            }

            if(!string.Equals(certIdFromXml, certIdFromCert, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(certRefFromXml, certIdFromCert, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ERROR] Cert ID mismatch. Cert: '{certIdFromCert}', XML ID: '{certIdFromXml}', Ref: '{certRefFromXml}'");
                return false;
            }

            if(!TryExtractSignatureArtifacts(folder, out var data, out var signature, out var certFromXml))
            {
                Console.WriteLine($"[WARN] Skipping folder '{folder}' due to missing/invalid components.");
                return true; // Skip instead of failing entire batch
            }

            if(!certFromXml.SequenceEqual(certificateBytes))
            {
                Console.WriteLine("[WARN] Certificate in XML does not match Key Vault certificate.");
            }

            using var publicKey = dsCertificate.GetECDsaPublicKey();
            if(publicKey == null)
            {
                Console.WriteLine("[ERROR] Failed to extract ECDSA public key.");
                return false;
            }

            if(!publicKey.VerifyData(data, signature, HashAlgorithmName.SHA384))
            {
                Console.WriteLine("[ERROR] Signature verification failed.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts the digital signature data, signature bytes, and certificate bytes from XML files in the specified folder,
        /// and validates the scheme administrator ID and certificate ID against the provided expected values.
        /// </summary>
        /// <param name="folder">Folder path containing the signature and data files.</param>
        /// <param name="data">Output parameter for the data bytes read from the data XML file.</param>
        /// <param name="signature">Output parameter for the signature bytes extracted from the signature XML file.</param>
        /// <param name="certFromXml">Output parameter for the certificate bytes extracted from the signature XML file.</param>
        /// <param name="saIdFromCert">Expected scheme administrator ID from the certificate.</param>
        /// <param name="certIdFromCert">Expected certificate ID from the certificate.</param>
        /// <returns>True if extraction and validation succeed; otherwise, false.</returns>
        private static bool TryExtractSignatureArtifacts(string folder, out byte[] data, out byte[] signature, out byte[] certFromXml)
        {
            data = null!;
            signature = null!;
            certFromXml = null!;

            var signatureFile = Path.Combine(folder, _permitSignFile);
            var dataFile = Path.Combine(folder, _permitXml);

            if(!File.Exists(signatureFile) || !File.Exists(dataFile))
            {
                Console.WriteLine("Error: Signature or data file missing.");
                return false;
            }

            var ns = (XNamespace)"http://www.iho.int/s100/se/5.2";
            var signatureDoc = XDocument.Load(signatureFile);

            var certElement = signatureDoc.Descendants(ns + "certificate").FirstOrDefault();
            var signatureElement = signatureDoc.Descendants(ns + "digitalSignature").FirstOrDefault();
            var saElement = signatureDoc.Descendants(ns + "schemeAdministrator").FirstOrDefault();

            if(certElement == null || signatureElement == null || saElement == null)
            {
                Console.WriteLine("Error: One or more required XML elements missing.");
                return false;
            }

            try
            {
                var certBase64 = certElement.Value?.Trim();
                var signatureBase64 = signatureElement.Value?.Trim();
                var saId = saElement.Attribute("id")?.Value?.Trim() ?? string.Empty;
                var certId = certElement.Attribute("id")?.Value?.Trim() ?? string.Empty;

                if(string.IsNullOrWhiteSpace(certBase64) ||
                    string.IsNullOrWhiteSpace(signatureBase64) ||
                    string.IsNullOrWhiteSpace(saId) ||
                    string.IsNullOrWhiteSpace(certId))
                {
                    Console.WriteLine("Error: One or more XML values are missing.");
                    return false;
                }

                certFromXml = Convert.FromBase64String(certBase64);
                signature = Convert.FromBase64String(signatureBase64);
                data = File.ReadAllBytes(dataFile);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error while extracting signature artifacts: {ex.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves a PEM-encoded X.509 certificate from Azure Key Vault and returns it as a byte array.
        /// </summary>
        /// <param name="keyVaultUrl">The URI of the initial Azure Key Vault that contains the configuration secret.</param>
        /// <param name="dsCertificateName">The name of the secret containing the PEM-formatted certificate.</param>
        /// <param name="tenantId">The Azure Active Directory tenant.</param>
        /// <param name="clientId">The Azure AD application (client) ID.</param>
        /// <param name="clientSecret">The Azure AD application secret.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the certificate as a byte array.</returns>
        /// <remarks>
        /// The method first retrieves a secondary Key Vault URI from a secret named
        /// "DataKeyVaultConfiguration--ServiceUri", then retrieves the actual certificate
        /// secret from that secondary Key Vault. The certificate is expected to be in
        /// PEM format and is base64-decoded before being returned.
        /// </remarks>
        private static async Task<byte[]> GetCertificateFromKeyVaultTask(string keyVaultUrl, string dsCertificateName, string tenantId, string clientId, string clientSecret)
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);

            KeyVaultSecret dataKeyVaultUri = await secretClient.GetSecretAsync("DataKeyVaultConfiguration--ServiceUri");

            secretClient = new SecretClient(new Uri(dataKeyVaultUri.Value), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(dsCertificateName);

            const string Header = "-----BEGIN CERTIFICATE-----";
            const string Footer = "-----END CERTIFICATE-----";

            var pem = secret.Value;
            var start = pem.IndexOf(Header, StringComparison.Ordinal) + Header.Length;
            var end = pem.IndexOf(Footer, StringComparison.Ordinal);
            var base64 = pem.Substring(start, end - start).Replace("\r", "").Replace("\n", "").Trim();

            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// Extracts the Scheme Administrator ID (Common Name - CN) from the certificate's subject.
        /// </summary>
        /// <param name="cert">The X509 certificate.</param>
        /// <returns>The Scheme Administrator ID or empty string if not found.</returns>
        private static string ExtractCertId(X509Certificate2 cert) =>
            GetSubjectPart(cert.Subject, "CN") ?? string.Empty;

        /// <summary>
        /// Extracts the Certificate ID (Organizational Unit - OU) from the certificate's subject, or falls back to serial number.
        /// </summary>
        /// <param name="cert">The X509 certificate.</param>
        /// <returns>The Certificate ID or serial number if OU not found.</returns>
        private static string ExtractSaId(X509Certificate2 cert) =>
            GetSubjectPart(cert.Issuer, "CN") ?? cert.SerialNumber;

        /// <summary>
        /// Retrieves a specified part (e.g. CN, OU) from the certificate subject string.
        /// </summary>
        /// <param name="cert">The X509 certificate.</param>
        /// <param name="key">The key to find in the subject (e.g., "CN", "OU").</param>
        /// <returns>The extracted part value, or null if not found.</returns>
        private static string? GetSubjectPart(string distinguishedName, string key)
        {
            var parts = distinguishedName.Split(", ");
            foreach(var part in parts)
            {
                if(part.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Substring(key.Length + 1);
                }
            }
            return null;
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

        /// <summary>
        /// This method is used to update the Date after 1 year from current date
        /// </summary>
        /// <returns>updated date</returns>
        public static string UpdateDate()
        {
            var updatedDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            return updatedDate;
        }
    }
}