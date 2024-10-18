using FakeItEasy;
using FluentAssertions;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Permits;

namespace UKHO.S100PermitService.Common.UnitTests.IO
{
    [TestFixture]
    public partial class PermitReaderWriterTests
    {
        private ISchemaValidator _fakeSchemaValidator;
        private IPermitReaderWriter _permitReaderWriter;

        [SetUp]
        public void Setup()
        {
            _fakeSchemaValidator = A.Fake<ISchemaValidator>();

            _permitReaderWriter = new PermitReaderWriter(_fakeSchemaValidator);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullSchemaValidator = () => new PermitReaderWriter(null);
            nullSchemaValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("schemaValidator");
        }

        [Test]
        public void WhenProductModelIsPassed_ThenReturnXmlString()
        {
            A.CallTo(() => _fakeSchemaValidator.ValidateSchema(A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var result = _permitReaderWriter.CreatePermits(GetPermitDetails());

            result.Should().NotBeNull();

            var stringResult = ConvertMemoryStreamToXmlString(result);
            var trimmedResult = TrimXml().Replace(stringResult, string.Empty);
            trimmedResult.Should().Be(GetExpectedXmlString());
        }

        [Test]
        public void WhenSchemaIsInvalid_ThenReturnsFalse()
        {
            A.CallTo(() => _fakeSchemaValidator.ValidateSchema(A<string>.Ignored, A<string>.Ignored)).Returns(false);

            FluentActions.Invoking(() => _permitReaderWriter.CreatePermits(GetInValidPermitDetails())).Should().
                                            ThrowExactly<PermitServiceException>().WithMessage("Invalid permit xml schema");
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
                             Expiry = DateTime.Parse("2024-09-02"),
                             Filename = "fakefilename",
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
                             Expiry = DateTime.Parse("2024-09-02"),
                             Filename = "fakefilename",
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
                             Expiry = DateTime.Parse("2024-09-02"),
                             Filename = "fakefilename",
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

        private string ConvertMemoryStreamToXmlString(MemoryStream memoryStream)
        {
            using var archive = new ZipArchive(memoryStream);
            var entry = archive.Entries[0];
            if(entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            else
            {
                return string.Empty;
            }
        }

        private string GetExpectedXmlString()
        {
            var expectedResult = "<?xmlversion=\"1.0\"encoding=\"UTF-8\"standalone=\"yes\"?><Permitxmlns:S100SE=\"http://www.iho.int/s100/se/5.2\"xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\"xmlns=\"http://www.iho.int/s100/se/5.2\">";
            expectedResult += "<S100SE:header><S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName><S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier><S100SE:version>1</S100SE:version>";
            expectedResult += "<S100SE:userpermit>fakeUserPermit1</S100SE:userpermit></S100SE:header><S100SE:products><S100SE:productid=\"fakeID1\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename><S100SE:editionNumber>1</S100SE:editionNumber>";
            expectedResult += "<S100SE:expiry>2024-09-02</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product><S100SE:productid=\"fakeID2\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename>";
            expectedResult += "<S100SE:editionNumber>1</S100SE:editionNumber><S100SE:expiry>2024-09-02</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product></S100SE:products></Permit>";

            return expectedResult;
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex TrimXml();
    }
}