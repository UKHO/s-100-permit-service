using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Services;

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
        private IOptions<ProductKeyServiceApiConfiguration> _fakeProductKeyServiceApiConfiguration;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private IPermitService _permitService;

        [SetUp]
        public void Setup()
        {
            _fakePermitReaderWriter = A.Fake<IPermitReaderWriter>();
            _fakeLogger = A.Fake<ILogger<PermitService>>();
            _fakeHoldingsService = A.Fake<IHoldingsService>();
            _fakeUserPermitService = A.Fake<IUserPermitService>();
            _fakeProductKeyService = A.Fake<IProductKeyService>();
            _fakeIs100Crypt = A.Fake<IS100Crypt>();
            _fakeProductKeyServiceApiConfiguration = Options.Create(new ProductKeyServiceApiConfiguration() { PermitHardwareId = "FAKE583E6CB6F32FD0B0648AF006A2BD" });

            _permitService = new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService,
                                                _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullPermitReaderWriter = () => new PermitService(null, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration);
            nullPermitReaderWriter.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitReaderWriter");

            Action nullLogger = () => new PermitService(_fakePermitReaderWriter, null, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullHoldingsService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, null, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration);
            nullHoldingsService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsService");

            Action nullUserPermitService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, null, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration);
            nullUserPermitService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitService");

            Action nullProductKeyService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, null, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration);
            nullProductKeyService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyService");

            Action nullIs100Crypt = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, null, _fakeProductKeyServiceApiConfiguration);
            nullIs100Crypt.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("s100Crypt");

            Action nullProductKeyServiceApiConfiguration = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, null);
            nullProductKeyServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyServiceApiConfiguration");
        }

        [Test]
        public async Task WhenPermitXmlHasValue_ThenFileIsCreated()
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                                     .Returns(new UserPermitServiceResponse() { LicenceId = 1 , UserPermits = [new UserPermit() { Title = "test", Upn = "1234567" }] });
            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                                    .Returns([new() { Cells = [new() { CellCode = "test101", CellTitle = "test", LatestEditionNumber = "1", LatestUpdateNumber = "1" }], }]);
            A.CallTo(() => _fakeProductKeyService.GetPermitKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                                    .Returns([new() { ProductName = "test101", Edition = "1", Key = "123456" }]);
            A.CallTo(() => _fakeIs100Crypt.GetEncKeysFromPermitKeys(A<List<ProductKeyServiceResponse>>.Ignored, A<string>.Ignored))
                                                    .Returns([new() { ProductName = "test101", Edition = "1", EncKey = "654321", Key = "123456" }]);
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
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.XmlSerializationEnd.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization completed"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.FileCreationEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml file created"
           ).MustHaveHappenedOnceExactly();

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
                                                     .Returns(new UserPermitServiceResponse() { LicenceId = 1 , UserPermits = [new UserPermit() { Title = "test", Upn = "1234567" }] });
            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                                    .Returns([new() { Cells = [new() { CellCode = "test101", CellTitle = "test", LatestEditionNumber = "1", LatestUpdateNumber = "1" }], }]);
            A.CallTo(() => _fakeProductKeyService.GetPermitKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                                    .Returns([new() { ProductName = "test101", Edition = "1", Key = "123456" }]);
            A.CallTo(() => _fakeIs100Crypt.GetEncKeysFromPermitKeys(A<List<ProductKeyServiceResponse>>.Ignored, A<string>.Ignored))
                                                    .Returns([new() { ProductName = "test101", Edition = "1", EncKey = "654321", Key = "123456" }]);
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
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.XmlSerializationEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization completed"
           ).MustHaveHappenedOnceExactly();

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
    }
}