using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Validation;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class PermitServiceTests
    {
        private ILogger<PermitService> _fakeLogger;
        private IPermitReaderWriter _fakePermitReaderWriter;
        private IHoldingsService _fakeHoldingsService;
        private IUserPermitService _fakeUserPermitService;
        private IProductKeyService _fakeProductKeyService;
        private IS100Crypt _fakeIs100Crypt;
        private IUserPermitValidator _fakeUserPermitValidator;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private PermitService _permitService;

        [SetUp]
        public void Setup()
        {
            _fakePermitReaderWriter = A.Fake<IPermitReaderWriter>();
            _fakeLogger = A.Fake<ILogger<PermitService>>();
            _fakeHoldingsService = A.Fake<IHoldingsService>();
            _fakeUserPermitService = A.Fake<IUserPermitService>();
            _fakeProductKeyService = A.Fake<IProductKeyService>();
            _fakeIs100Crypt = A.Fake<IS100Crypt>();
            _fakeUserPermitValidator = A.Fake<IUserPermitValidator>();

            _permitService = new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullPermitReaderWriter = () => new PermitService(null, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt);
            nullPermitReaderWriter.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitReaderWriter");

            Action nullLogger = () => new PermitService(_fakePermitReaderWriter, null, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullHoldingsService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, null, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt);
            nullHoldingsService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsService");

            Action nullUserPermitService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, null, _fakeProductKeyService, _fakeIs100Crypt);
            nullUserPermitService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitService");

            Action nullProductKeyService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, null, _fakeIs100Crypt);
            nullProductKeyService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyService");

            Action nullIs100Crypt = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, null);
            nullIs100Crypt.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("s100Crypt");
        }

        [Test]
        public async Task WhenPermitXmlHasValue_ThenFileIsCreated()
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GeUserPermitServiceResponse());

            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored)).Returns(true);

            A.CallTo(() => _fakeUserPermitService.MapUserPermitResponse(A<UserPermitServiceResponse>.Ignored)).Returns(GetUpnInfo());

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns([new() { Cells = [new() { CellCode = "test101", CellTitle = "", LatestEditionNumber = "1", LatestUpdateNumber = "1" }], }]);

            A.CallTo(() => _fakeProductKeyService.GetPermitKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns([new() { ProductName = "test101", Edition = "1", Key = "123456" }]);

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedHardwareIdFromUserPermit(A<List<UpnInfo>>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());
            A.CallTo(() => _fakePermitReaderWriter.ReadPermit(A<Permit>.Ignored)).Returns("fakepermit");

            await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            A.CallTo(() => _fakePermitReaderWriter.WritePermit(A<string>.Ignored)).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit completed"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.XmlSerializationStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization started"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.XmlSerializationEnd.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization completed"
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.FileCreationEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml file created"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Error
           && call.GetArgument<EventId>(1) == EventIds.EmptyPermitXml.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Empty permit xml is received"
           ).MustNotHaveHappened();
        }

        [Test]
        public async Task WhenEmptyPermitXml_ThenFileIsNotCreated()
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GeUserPermitServiceResponse());

            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored)).Returns(true);

            A.CallTo(() => _fakeUserPermitService.MapUserPermitResponse(A<UserPermitServiceResponse>.Ignored)).Returns(GetUpnInfo());

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns([new() { Cells = [new() { CellCode = "test101", CellTitle = "", LatestEditionNumber = "1", LatestUpdateNumber = "1" }], }]);

            A.CallTo(() => _fakeProductKeyService.GetPermitKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns([new() { ProductName = "test101", Edition = "1", Key = "123456" }]);

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedHardwareIdFromUserPermit(A<List<UpnInfo>>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());

            A.CallTo(() => _fakePermitReaderWriter.ReadPermit(A<Permit>.Ignored)).Returns("");

            await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            A.CallTo(() => _fakePermitReaderWriter.WritePermit(A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit completed"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.XmlSerializationStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization started"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.XmlSerializationEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization completed"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Error
           && call.GetArgument<EventId>(1) == EventIds.EmptyPermitXml.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Empty permit xml is received"
           ).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.FileCreationEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Xml file created"
           ).MustNotHaveHappened();
        }

        private static List<UpnInfo> GetUpnInfo()
        {
            return
            [
                new UpnInfo()
                {
                    EncryptedHardwareId = "FE5A853DEF9E83C9FFEF5AA001478103",
                    Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3",
                    MId = "A1B2C3",
                    Crc32 = "DB74C038"
                },
                new UpnInfo()
                {
                    EncryptedHardwareId = "869D4E0E902FA2E1B934A3685E5D0E85",
                    Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6",
                    MId = "D4E5F6",
                    Crc32 = "C1FDEC8B"
                }
            ];
        }

        private static List<UpnInfo> GetUpnInfoWithDecryptedHardwareId()
        {
            return
            [
                new UpnInfo()
                {
                    HardwareId = "86C520323CEA3056B5ED7000F98814CB",
                    EncryptedHardwareId = "FE5A853DEF9E83C9FFEF5AA001478103",
                    Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3",
                    MId = "A1B2C3",
                    Crc32 = "DB74C038"
                },
                new UpnInfo()
                {
                    HardwareId = "B2C0F91ADAAEA51CC5FCCA05C47499E4",
                    EncryptedHardwareId = "869D4E0E902FA2E1B934A3685E5D0E85",
                    Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6",
                    MId = "D4E5F6",
                    Crc32 = "C1FDEC8B"
                }
            ];
        }

        private static UserPermitServiceResponse GeUserPermitServiceResponse()
        {
            return new UserPermitServiceResponse()
            {
                LicenceId = 1,
                UserPermits = [ new UserPermit{ Title = "Aqua Radar", Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FF8J0K1L2" },
                    new UserPermit{  Title= "SeaRadar X", Upn = "E9FAE304D230E4C729288349DA29776EE9B57E01M3N4O5" },
                    new UserPermit{ Title = "Navi Radar", Upn = "F1EB202BDC150506E21E3E44FD1829424462D958P6Q7R8" }
                ]
            };
        }
    }
}