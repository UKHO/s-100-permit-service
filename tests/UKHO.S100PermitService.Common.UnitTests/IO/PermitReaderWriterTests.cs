using FakeItEasy;
using FluentAssertions;
using System.IO.Abstractions;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Permits;

namespace UKHO.S100PermitService.Common.UnitTests.IO
{
    [TestFixture]
    public class PermitReaderWriterTests
    {
        private IFileSystem _fakeFileSystem;
        private PermitReaderWriter _fakePermitReaderWriter;

        [SetUp]
        public void Setup()
        {
            _fakeFileSystem = A.Fake<IFileSystem>();
            _fakePermitReaderWriter = new PermitReaderWriter(_fakeFileSystem);
        }

        [Test]
        public void WhenProductModelIsPassed_ThenReturnXmlString()
        {
            var expectedResult = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Permit xmlns:S100SE=\"http://www.iho.int/s100/se/5.1\" xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\" xmlns=\"http://www.iho.int/s100/se/5.0\">\r\n  <S100SE:header>\r\n    <S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate>\r\n    <S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName>\r\n    <S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier>\r\n    <S100SE:version>1</S100SE:version>\r\n    <S100SE:userpermit>fakeUserPermit</S100SE:userpermit>\r\n  </S100SE:header>\r\n  <S100SE:products>\r\n    <S100SE:product id=\"fakeID\">\r\n      <S100SE:datasetPermit>\r\n        <S100SE:filename>fakefilename</S100SE:filename>\r\n        <S100SE:editionNumber>1</S100SE:editionNumber>\r\n        <S100SE:expiry>2024-09-02</S100SE:expiry>\r\n        <S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey>\r\n      </S100SE:datasetPermit>\r\n    </S100SE:product>\r\n  </S100SE:products>\r\n</Permit>";
            var products = new List<Products>()
            {
                new()
                {
                     Id = "fakeID",
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
            var permit = new Permit()
            {
                Header = new Header()
                {
                    DataServerIdentifier = "fakeDataServerIdentifier",
                    DataServerName = "fakeDataServerName",
                    IssueDate = "2024-09-02+01:00",
                    Userpermit = "fakeUserPermit",
                    Version = "1"
                },
                Products = products.ToArray()
            };

            var result = _fakePermitReaderWriter.ReadPermit(permit);

            result.Should().Be(expectedResult);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_FileSystem_Parameter_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
             () => new PermitReaderWriter(null))
             .ParamName
             .Should().Be("fileSystem");
        }
    }
}
