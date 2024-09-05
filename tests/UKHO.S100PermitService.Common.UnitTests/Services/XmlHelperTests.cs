using FluentAssertions;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class XmlHelperTests
    {
        private XmlHelper _fakePermitXmlService;

        [SetUp]
        public void Setup()
        {
            _fakePermitXmlService = new XmlHelper();
        }

        [Test]
        public void WhenProductModelIsPassed_ThenReturnXmlString()
        {
            var expectedResult = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Permit xmlns:S100SE=\"http://www.iho.int/s100/se/5.1\" xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\" xmlns=\"http://www.iho.int/s100/se/5.0\">\r\n  <S100SE:header>\r\n    <S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate>\r\n    <S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName>\r\n    <S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier>\r\n    <S100SE:version>1</S100SE:version>\r\n    <S100SE:userpermit>fakeUserPermit</S100SE:userpermit>\r\n  </S100SE:header>\r\n  <S100SE:products>\r\n    <S100SE:product id=\"fakeID\">\r\n      <S100SE:datasetPermit>\r\n        <S100SE:filename>fakefilename</S100SE:filename>\r\n        <S100SE:editionNumber>1</S100SE:editionNumber>\r\n        <S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate>\r\n        <S100SE:expiry>2024-09-02</S100SE:expiry>\r\n        <S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey>\r\n      </S100SE:datasetPermit>\r\n    </S100SE:product>\r\n  </S100SE:products>\r\n</Permit>";
            var products = new List<products>()
            {
                new products()
                {
                     id = "fakeID",
                     datasetPermit = new productsProductDatasetPermit[]
                     {
                         new productsProductDatasetPermit() 
                         {
                             issueDate = "2024-09-02+01:00",
                             editionNumber = 1,
                             encryptedKey = "fakeencryptedkey",
                             expiry = DateTime.Parse("2024-09-02"),
                             filename = "fakefilename",
                         }
                     }
                }
            };
            var permit = new Permit()
            {
                header = new header()
                {
                    dataServerIdentifier = "fakeDataServerIdentifier",
                    dataServerName = "fakeDataServerName",
                    issueDate = "2024-09-02+01:00",
                    userpermit = "fakeUserPermit",
                    version = 1
                },
                products = products.ToArray()
            };

            var result = _fakePermitXmlService.GetPermitXmlString(permit);

            result.Should().Be(expectedResult);
        }
    }
}
