using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.Request;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class PermitServiceTests
    {
        private ILogger<PermitService> _fakeLogger;
        private IPermitReaderWriter _fakePermitReaderWriter;
        private IProductKeyService _fakeProductKeyService;
        private IS100Crypt _fakeIs100Crypt;
        private IOptions<ProductKeyServiceApiConfiguration> _fakeProductKeyServiceApiConfiguration;
        private IOptions<PermitFileConfiguration> _fakePermitFileConfiguration;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        private const string PRODUCT_TYPE = "s100";
        private IPermitService _permitService;

        [SetUp]
        public void Setup()
        {
            _fakePermitReaderWriter = A.Fake<IPermitReaderWriter>();
            _fakeLogger = A.Fake<ILogger<PermitService>>();
            _fakeProductKeyService = A.Fake<IProductKeyService>();
            _fakeIs100Crypt = A.Fake<IS100Crypt>();
            _fakeProductKeyServiceApiConfiguration = Options.Create(new ProductKeyServiceApiConfiguration() { HardwareId = "FAKE583E6CB6F32FD0B0648AF006A2BD" });
            _fakePermitFileConfiguration = A.Fake<IOptions<PermitFileConfiguration>>();

            _permitService = new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService,
                                                _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullPermitReaderWriter = () => new PermitService(null, _fakeLogger, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullPermitReaderWriter.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitReaderWriter");

            Action nullLogger = () => new PermitService(_fakePermitReaderWriter, null, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullProductKeyService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, null, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullProductKeyService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyService");

            Action nullIs100Crypt = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService, null, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullIs100Crypt.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("s100Crypt");

            Action nullProductKeyServiceApiConfiguration = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService, _fakeIs100Crypt, null, _fakePermitFileConfiguration);
            nullProductKeyServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyServiceApiConfiguration");

            Action nullPermitFileConfiguration = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, null);
            nullPermitFileConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitFileConfiguration");
        }

        [Test]
        public async Task WhenPermitXmlHasValue_ThenFileIsCreated()
        {
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(GetExpectedXmlString()));

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<IEnumerable<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                                                .Returns(ServiceResponseResult<IEnumerable<ProductKeyServiceResponse>>.Success([
                                                            new() { ProductName = "CellCode", Edition = "1", Key = "123456" },
                                                        new() { ProductName = "CellCode1", Edition = "2", Key = "7891011" }]));

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedKeysFromProductKeysAsync(A<IEnumerable<ProductKeyServiceResponse>>.Ignored, A<string>.Ignored))
                .Returns(GetDecryptedKeysFromProductKeys());

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedHardwareIdFromUserPermitAsync(A<IEnumerable<UserPermit>>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());

            A.CallTo(() => _fakePermitReaderWriter.ReadXsdVersion()).Returns("5.2.0");

            A.CallTo(() => _fakeIs100Crypt.CreateEncryptedKeyAsync(A<string>.Ignored, A<string>.Ignored)).Returns("123456");

            A.CallTo(() => _fakePermitReaderWriter.CreatePermitZipAsync(A<Dictionary<string, Permit>>.Ignored)).Returns(expectedStream);

            var response = await _permitService.ProcessPermitRequestAsync(PRODUCT_TYPE, GetPermitRequestDetails(), _fakeCorrelationId, CancellationToken.None);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Value.Length.Should().Be(expectedStream.Length);

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started for ProductType {productType}."
             ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed for ProductType {productType}."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.ServiceUnavailable)]
        public async Task WhenProductKeyServiceReturnsOtherThanOk_ThenPermitFileNoCreated(HttpStatusCode httpStatusCode)
        {
            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<IEnumerable<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                                                .Returns(ServiceResponseResult<IEnumerable<ProductKeyServiceResponse>>.Failure(httpStatusCode));

            var response = await _permitService.ProcessPermitRequestAsync(PRODUCT_TYPE, GetPermitRequestDetails(), _fakeCorrelationId, CancellationToken.None);

            response.StatusCode.Should().Be(httpStatusCode);

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started for ProductType {productType}."
             ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed for ProductType {productType}."
            ).MustNotHaveHappened();
        }

        private static PermitRequest GetPermitRequestDetails()
        {
            return new PermitRequest()
            {
                Products = new List<Product>
                {
                    new() {
                        ProductName = "CellCode",
                        EditionNumber = 1,
                        PermitExpiryDate = "2025-04-02"
                    },
                    new() {
                        ProductName = "CellCode1",
                        EditionNumber = 2,
                        PermitExpiryDate = "2025-04-02"
                    }
                },
                UserPermits = new List<UserPermit>
                {
                    new() {
                        Title = "IHO Test System",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                    },
                    new() {
                        Title = "OeM Test 1",
                        Upn = "7B5CED73389DECDB110E6E803F957253F0DE13D1G7H8I9"
                    }
                }
            };
        }

        private static List<UpnInfo> GetUpnInfoWithDecryptedHardwareId()
        {
            return
            [
                new UpnInfo()
                {
                    Title = "FakeTitle1",
                    DecryptedHardwareId = "86C520323CEA3056B5ED7000F98814CB",
                    Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                },
                new UpnInfo()
                {
                    Title = "FakeTitle2",
                    DecryptedHardwareId = "B2C0F91ADAAEA51CC5FCCA05C47499E4",
                    Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                }
            ];
        }

        private static IEnumerable<ProductKey> GetDecryptedKeysFromProductKeys()
        {
            return
            [
                new ProductKey()
                {
                    ProductName = "CellCode",
                    Edition = "1",
                    Key = "123456",
                    DecryptedKey = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                },
                new ProductKey()
                {
                    ProductName = "CellCode1",
                    Edition = "86C520323CEA3056B5ED7000F98814CB",
                    Key = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3",
                    DecryptedKey = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                }
            ];
        }

        private string GetExpectedXmlString()
        {
            var sb = new StringBuilder();
            sb.Append("<?xmlversion=\"1.0\"encoding=\"UTF-8\"standalone=\"yes\"?><Permitxmlns:S100SE=\"http://www.iho.int/s100/se/5.2\"xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\"xmlns=\"http://www.iho.int/s100/se/5.2\"><S100SE:header>");
            sb.Append("<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName><S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier><S100SE:version>1</S100SE:version>");
            sb.Append("<S100SE:userpermit>fakeUserPermit</S100SE:userpermit></S100SE:header><S100SE:products><S100SE:productid=\"fakeID\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename><S100SE:editionNumber>1</S100SE:editionNumber>");
            sb.Append("<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:expiry>2024-09-02</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product></S100SE:products></Permit>");

            return sb.ToString();
        }
    }
}