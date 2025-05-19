using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.PermitSign;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Transformer;

namespace UKHO.S100PermitService.Common.UnitTests.Transformer
{
    [TestFixture]
    public class XmlTransformerTests
    {
        private ISchemaValidator _fakeSchemaValidator;
        private ILogger<DigitalSignatureProvider> _fakeLogger;
        private XmlTransformer _xmlTransformer;

        [SetUp]
        public void Setup()
        {
            _fakeSchemaValidator = A.Fake<ISchemaValidator>();
            _fakeLogger = A.Fake<ILogger<DigitalSignatureProvider>>();
            _xmlTransformer = new XmlTransformer(_fakeLogger, _fakeSchemaValidator);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var nullLogger = Assert.Throws<ArgumentNullException>(() => new XmlTransformer(null, _fakeSchemaValidator));
            Assert.That(nullLogger.ParamName, Is.EqualTo("logger"));

            var nullSchemaValidator = Assert.Throws<ArgumentNullException>(() => new XmlTransformer(_fakeLogger, null));
            Assert.That(nullSchemaValidator.ParamName, Is.EqualTo("schemaValidator"));
        }

        [Test]
        public async Task When_ValidObjectForStandaloneDigitalSignatureIsSerialized_Then_XmlStringIsReturned()
        {
            var expected = GetExpectedPermitSignString();

            A.CallTo(() => _fakeSchemaValidator.ValidateSchema(A<string>._, A<string>._)).Returns(true);

            var result = await _xmlTransformer.SerializeToXml(GetValidDigitalSignature());

            string Normalize(string xml) => xml.Replace("\r", "").Replace("\n", "").Replace("  ", "").Trim();

            Assert.That(result, Is.Not.Null.And.Not.Empty.And.Not.EqualTo(string.Empty));

            Assert.That(Normalize(result), Is.EqualTo(Normalize(expected)));

            A.CallTo(() => _fakeSchemaValidator.ValidateSchema(A<string>._, A<string>._)).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.XMLSerializationStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "XML serialization process started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.XMLSerializationCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "XML serialization process completed."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSchemaIsInvalidForStandaloneDigitalSignature_ThenThrowsPermitServiceException()
        {
            A.CallTo(() => _fakeSchemaValidator.ValidateSchema(A<string>.Ignored, A<string>.Ignored)).Returns(false);

            var ex = Assert.ThrowsAsync<PermitServiceException>(async () => await _xmlTransformer.SerializeToXml(GetInValidDigitalSignature()));

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Is.EqualTo("Invalid permit xml schema."));

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.XMLSerializationStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "XML serialization process started."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSchemaIsInvalidForPermitDetails_ThenThrowsPermitServiceException()
        {
            A.CallTo(() => _fakeSchemaValidator.ValidateSchema(A<string>.Ignored, A<string>.Ignored)).Returns(false);

            var ex = Assert.ThrowsAsync<PermitServiceException>(async () => await _xmlTransformer.SerializeToXml(GetInvalidValidPermit()));

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Is.EqualTo("Invalid permit xml schema."));

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.XMLSerializationStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "XML serialization process started."
            ).MustHaveHappenedOnceExactly();
        }

        private StandaloneDigitalSignature GetValidDigitalSignature()
        {
            return new StandaloneDigitalSignature()
            {
                Filename = PermitServiceConstants.PermitXmlFileName,
                Certificates = new Certificates
                {
                    SchemeAdministrator = new SchemeAdministrator { Id = "issuer" },
                    Certificate =
                        new Certificate { Id = "certificateDsId", Issuer = "issuer", Value = "certificateValue" }
                },
                DigitalSignature = new DigitalSignature
                {
                    Id = PermitServiceConstants.DigitalSignatureId,
                    CertificateRef = "certificateDsId",
                    Value = "signatureValue"
                }
            };
        }

        private StandaloneDigitalSignature GetInValidDigitalSignature()
        {
            return new StandaloneDigitalSignature()
            {
                Filename = PermitServiceConstants.PermitXmlFileName,
                Certificates = new Certificates
                {
                    SchemeAdministrator = new SchemeAdministrator { Id = "issuer" },
                    Certificate = new Certificate
                    {
                        Id = "certificateDsId",
                        Issuer = "issuer",
                        Value = "certificateValue"
                    }
                },
                DigitalSignature = new DigitalSignature { }
            };
        }

        private Permit GetInvalidValidPermit()
        {
            var product = new Products()
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
            };
            return new Permit()
            {
                Header = new Header()
                {
                },
                Products = [product]
            };
        }

        private string GetExpectedPermitSignString()
        {
            var expectedResult = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
            expectedResult += "<S100SE:StandaloneDigitalSignature xmlns:S100SE=\"http://www.iho.int/s100/se/5.2\" xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\" xmlns=\"http://www.iho.int/s100/se/5.2\">";
            expectedResult += "<S100SE:filename>PERMIT.XML</S100SE:filename>";
            expectedResult += "<S100SE:certificates>";
            expectedResult += "<S100SE:schemeAdministrator id=\"issuer\" />";
            expectedResult += "<S100SE:certificate id=\"certificateDsId\" issuer=\"issuer\">";
            expectedResult += "certificateValue";
            expectedResult += "</S100SE:certificate>";
            expectedResult += "</S100SE:certificates>";
            expectedResult += "<S100SE:digitalSignature id=\"permit\" certificateRef=\"certificateDsId\">";
            expectedResult += "signatureValue";
            expectedResult += "</S100SE:digitalSignature>";
            expectedResult += "</S100SE:StandaloneDigitalSignature>";

            return expectedResult;
        }
    }
}