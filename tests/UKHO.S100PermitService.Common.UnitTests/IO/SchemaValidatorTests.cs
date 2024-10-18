using FluentAssertions;
using System.Reflection;
using UKHO.S100PermitService.Common.IO;

namespace UKHO.S100PermitService.Common.UnitTests.IO
{
    [TestFixture]
    public class SchemaValidatorTests
    {
        private const string SchemaFile = @"XmlSchema\Permit_Schema.xsd";
        private readonly string _xsdPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, SchemaFile);

        private ISchemaValidator _schemaValidator;

        [SetUp]
        public void SetUp()
        {
            _schemaValidator = new SchemaValidator();
        }

        [Test]
        public void WhenSchemaIsInvalid_ThenReturnsFalse()
        {
            var fakeInvalidPermit = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Per xmlns:S100SE=\"http://www.iho.int/s100/se/5.2\" xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\" xmlns=\"http://www.iho.int/s100/se/5.2\">\r\n";
            fakeInvalidPermit += "  <S100SE:header>\r\n     <S100SE:Name>0akeDataServerName</S100SE:Name>\r\n    <S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier>\r\n    <S100SE:version>1</S100SE:version>\r\n    <S100SE:userpermit>fakeUserPermit</S100SE:userpermit>\r\n";
            fakeInvalidPermit += "  </S100SE:header>\r\n  <S100SE:products>\r\n    <S100SE:product id=\"fakeID\">\r\n      <S100SE:datasetPermit>\r\n        <S100SE:filesname>fakefilename</S100SE:filesname>\r\n            <S100SE:expiry>2024-09-02</S100SE:expiry>\r\n";
            fakeInvalidPermit += "        <S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey>\r\n      </S100SE:datasetPermit>\r\n    </S100SE:product>\r\n   <S100SE:product id=\"fakeID2\">\r\n      <S100SE:datasetPermit>\r\n        <S100SE:filesname>fakefilename</S100SE:filesname>\r\n            <S100SE:expiry>2024-09-02</S100SE:expiry>\r\n        <S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey>\r\n      </S100SE:datasetPermit>\r\n    </S100SE:product>\r\n </S100SE:products>\r\n</Per>";

            var result = _schemaValidator.ValidateSchema(fakeInvalidPermit, _xsdPath);

            result.Should().Be(false);
        }

        [Test]
        public void WhenSchemaIsValid_ThenReturnsTrue()
        {
            var fakeValidPermit = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n<Permit xmlns:S100SE=\"http://www.iho.int/s100/se/5.2\" xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\" xmlns=\"http://www.iho.int/s100/se/5.2\">\r\n  <S100SE:header>\r\n";
            fakeValidPermit += "    <S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate>\r\n    <S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName>\r\n    <S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier>\r\n    <S100SE:version>1</S100SE:version>\r\n    <S100SE:userpermit>fakeUserPermit1</S100SE:userpermit>\r\n  </S100SE:header>\r\n  <S100SE:products>\r\n    <S100SE:product id=\"fakeID1\">\r\n";
            fakeValidPermit += "      <S100SE:datasetPermit>\r\n        <S100SE:filename>fakefilename</S100SE:filename>\r\n        <S100SE:editionNumber>1</S100SE:editionNumber>\r\n        <S100SE:expiry>2024-09-02</S100SE:expiry>\r\n        <S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey>\r\n      </S100SE:datasetPermit>\r\n    </S100SE:product>\r\n    <S100SE:product id=\"fakeID2\">\r\n      <S100SE:datasetPermit>\r\n        ";
            fakeValidPermit += "<S100SE:filename>fakefilename</S100SE:filename>\r\n        <S100SE:editionNumber>1</S100SE:editionNumber>\r\n        <S100SE:expiry>2024-09-02</S100SE:expiry>\r\n        <S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey>\r\n      </S100SE:datasetPermit>\r\n    </S100SE:product>\r\n  </S100SE:products>\r\n</Permit>";

            var result = _schemaValidator.ValidateSchema(fakeValidPermit, _xsdPath);

            result.Should().Be(true);
        }
    }
}