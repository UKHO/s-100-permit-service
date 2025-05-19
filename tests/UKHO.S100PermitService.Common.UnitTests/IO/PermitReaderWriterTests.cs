using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Transformer;

namespace UKHO.S100PermitService.Common.UnitTests.IO
{
    [TestFixture]
    public partial class PermitReaderWriterTests
    {
        private ILogger<PermitReaderWriter> _fakeLogger;
        private IPermitSignGeneratorService _fakePermitSignGeneratorService;
        private IXmlTransformer _fakeXmlTransformer;
        private IPermitReaderWriter _permitReaderWriter;

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<PermitReaderWriter>>();
            _fakePermitSignGeneratorService = A.Fake<IPermitSignGeneratorService>();
            _fakeXmlTransformer = A.Fake<IXmlTransformer>();

            _permitReaderWriter = new PermitReaderWriter(_fakeLogger, _fakePermitSignGeneratorService, _fakeXmlTransformer);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var nullLogger = Assert.Throws<ArgumentNullException>(() => new PermitReaderWriter(null, _fakePermitSignGeneratorService, _fakeXmlTransformer));
            Assert.That(nullLogger.ParamName, Is.EqualTo("logger"));

            var nullPermitSignGeneratorService = Assert.Throws<ArgumentNullException>(() => new PermitReaderWriter(_fakeLogger, null, _fakeXmlTransformer));
            Assert.That(nullPermitSignGeneratorService.ParamName, Is.EqualTo("permitSignGeneratorService"));

            var nullXmlTransformer = Assert.Throws<ArgumentNullException>(() => new PermitReaderWriter(_fakeLogger, _fakePermitSignGeneratorService, null));
            Assert.That(nullXmlTransformer.ParamName, Is.EqualTo("xmlTransformer"));
        }

        [Test]
        public void WhenConstructorIsCalledWithNullDependencies_ThenShouldThrowArgumentNullException()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => new PermitReaderWriter(null, _fakePermitSignGeneratorService, _fakeXmlTransformer),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'logger')"));

                Assert.That(() => new PermitReaderWriter(_fakeLogger, null, _fakeXmlTransformer),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'permitSignGeneratorService')"));

                Assert.That(() => new PermitReaderWriter(_fakeLogger, _fakePermitSignGeneratorService, null),
                    Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'xmlTransformer')"));
            });
        }

        [Test]
        public void WhenPermitSchemaFileLocatedSuccessfully_ThenReturnsXsdVersion()
        {
            var result = _permitReaderWriter.ReadXsdVersion();

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task When_CreatePermitZipAsyncIsCalledWithValidPermits_Then_XmlAndSignFilesCreatedAndAddedToZip()
        {
            A.CallTo(() => _fakeXmlTransformer.SerializeToXml(A<Permit>.Ignored))
                .Returns(GetExpectedPermitXmlString());

            A.CallTo(() => _fakePermitSignGeneratorService.GeneratePermitSignXmlAsync(A<string>.Ignored))
                .Returns(GetExpectedPermitSignString());

            var result = await _permitReaderWriter.CreatePermitZipAsync(GetPermitDetails());

            Assert.IsNotNull(result);

            var xmlAndSignStrings = ConvertMemoryStreamToXmlAndSignStrings(result);

            Assert.That(xmlAndSignStrings, Does.ContainKey("TestTitle1/PERMIT.XML"));
            Assert.That(xmlAndSignStrings, Does.ContainKey("TestTitle1/PERMIT.SIGN"));
            Assert.That(xmlAndSignStrings, Does.ContainKey("TestTitle2/PERMIT.XML"));
            Assert.That(xmlAndSignStrings, Does.ContainKey("TestTitle2/PERMIT.SIGN"));

            var trimmedXml = TrimXml().Replace(xmlAndSignStrings["TestTitle1/PERMIT.XML"], string.Empty);
            Assert.That(trimmedXml, Is.EqualTo(GetExpectedPermitXmlString()));

            Assert.That(xmlAndSignStrings["TestTitle1/PERMIT.SIGN"], Is.EqualTo(GetExpectedPermitSignString()));

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitXmlCreationStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Creation of Permit XML for UPN: {UpnTitle} started."
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitXmlCreationCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Creation of Permit XML for UPN: {UpnTitle} completed."
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitSignCreationStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Creation of Permit SIGN for UPN: {UpnTitle} started."
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitSignCreationCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Creation of Permit SIGN for UPN: {UpnTitle} completed."
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PermitZipCreationCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Permit zip creation completed."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenPermitSignGenerationFails_ThenThrowsException()
        {
            A.CallTo(() => _fakePermitSignGeneratorService.GeneratePermitSignXmlAsync(A<string>.Ignored))
                .Throws(new Exception("Permit sign generation failed."));

            Assert.ThrowsAsync<Exception>(async () => await _permitReaderWriter.CreatePermitZipAsync(GetPermitDetails()));
        }

        private Dictionary<string, Permit> GetPermitDetails()
        {
            var fakePermitDictionary = new Dictionary<string, Permit>();

            var fakeProducts = new List<Products>()
            {
                new()
                {
                     Id = "fakeID1",
                     DatasetPermit =
                     [
                         new ProductsProductDatasetPermit()
                         {
                             EditionNumber = 1,
                             EncryptedKey = "fakeencryptedkey",
                             Expiry = DateTime.UtcNow.AddDays(10).ToString("yyyy-mm-dd"),
                             Filename = "fakefilename"
                         }
                     ]
                },
                new()
                {
                     Id = "fakeID2",
                     DatasetPermit =
                     [
                         new ProductsProductDatasetPermit()
                         {
                             EditionNumber = 1,
                             EncryptedKey = "fakeencryptedkey",
                             Expiry = DateTime.UtcNow.AddDays(10).ToString("yyyy-mm-dd"),
                             Filename = "fakefilename"
                         }
                     ]
                }
            };
            var fakePermit1 = new Permit()
            {
                Header = new Header()
                {
                    DataServerIdentifier = "fakeDataServerIdentifier",
                    DataServerName = "fakeDataServerName",
                    IssueDate = "2024-09-02+01:00",
                    Userpermit = "fakeUserPermit1",
                    Version = "1"
                },
                Products = [.. fakeProducts]
            };

            var fakePermit2 = new Permit()
            {
                Header = new Header()
                {
                    DataServerIdentifier = "fakeDataServerIdentifier",
                    DataServerName = "fakeDataServerName",
                    IssueDate = "2024-09-02+01:00",
                    Userpermit = "fakeUserPermit2",
                    Version = "1"
                },
                Products = [.. fakeProducts]
            };

            fakePermitDictionary.Add("TestTitle1", fakePermit1);
            fakePermitDictionary.Add("TestTitle2", fakePermit2);

            return fakePermitDictionary;
        }

        private Dictionary<string, Permit> GetInValidPermitDetails()
        {
            var fakePermitDictionary = new Dictionary<string, Permit>();

            var fakeProducts = new List<Products>()
            {
                new()
                {
                    Id = "fakeID1",
                    DatasetPermit =
                    [
                        new ProductsProductDatasetPermit()
                        {
                            EditionNumber = 1,
                            EncryptedKey = "fakeencryptedkey",
                            Expiry = DateTime.UtcNow.AddDays(10).ToString("yyyy-mm-dd"),
                            Filename = "fakefilename"
                        }
                    ]
                }
            };
            var fakePermit1 = new Permit()
            {
                Header = new Header()
                {
                },
                Products = [.. fakeProducts]
            };

            fakePermitDictionary.Add("TestTitle1", fakePermit1);

            return fakePermitDictionary;
        }

        private Dictionary<string, string> ConvertMemoryStreamToXmlAndSignStrings(Stream stream)
        {
            var result = new Dictionary<string, string>();

            using var archive = new ZipArchive(stream);

            foreach(var entry in archive.Entries)
            {
                if(entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(".sign", StringComparison.OrdinalIgnoreCase))
                {
                    using var entryStream = entry.Open();
                    using var reader = new StreamReader(entryStream, Encoding.UTF8);
                    var content = reader.ReadToEnd();

                    if(!string.IsNullOrWhiteSpace(content))
                    {
                        result[entry.FullName] = content;
                    }
                }
            }
            return result;
        }

        private string GetExpectedPermitXmlString()
        {
            var expectedResult = "<?xmlversion=\"1.0\"encoding=\"UTF-8\"standalone=\"yes\"?><Permitxmlns:S100SE=\"http://www.iho.int/s100/se/5.2\"xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\"xmlns=\"http://www.iho.int/s100/se/5.2\">";
            expectedResult += "<S100SE:header><S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName><S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier><S100SE:version>1</S100SE:version>";
            expectedResult += "<S100SE:userpermit>fakeUserPermit1</S100SE:userpermit></S100SE:header><S100SE:products><S100SE:productid=\"fakeID1\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename><S100SE:editionNumber>1</S100SE:editionNumber>";
            expectedResult += $"<S100SE:expiry>{DateTime.UtcNow.AddDays(10):yyyy-mm-dd}</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product><S100SE:productid=\"fakeID2\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename>";
            expectedResult += $"<S100SE:editionNumber>1</S100SE:editionNumber><S100SE:expiry>{DateTime.UtcNow.AddDays(10):yyyy-mm-dd}</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product></S100SE:products></Permit>";

            return expectedResult;
        }

        private string GetExpectedPermitSignString()
        {
            var expectedResult = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
            expectedResult += "<S100SE:StandaloneDigitalSignature xmlns:S100SE=\"http://www.iho.int/s100/se/5.2\" xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\" xmlns=\"http://www.iho.int/s100/se/5.2\">";
            expectedResult += "<S100SE:filename>PERMIT.XML</S100SE:filename>";
            expectedResult += "<S100SE:certificates>";
            expectedResult += "<S100SE:schemeAdministrator id=\"Permit Signing Authority\" />";
            expectedResult += "<S100SE:certificate id=\"Permit Data Signing\" issuer=\"Permit Signing Authority\">";
            expectedResult += "Certificate_Details";
            expectedResult += "</S100SE:certificate>";
            expectedResult += "</S100SE:certificates>";
            expectedResult += "<S100SE:digitalSignature id=\"permit\" certificateRef=\"Permit Data Signing\">";
            expectedResult += "SignDetails";
            expectedResult += "</S100SE:digitalSignature>";
            expectedResult += "</S100SE:StandaloneDigitalSignature>";

            return expectedResult;
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex TrimXml();
    }
}