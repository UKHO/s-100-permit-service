using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Validations;

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
        const string NoContent = "noContent";
        const string OkResponse = "okResponse";

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
            _fakeProductKeyServiceApiConfiguration = Options.Create(new ProductKeyServiceApiConfiguration() { HardwareId = "FAKE583E6CB6F32FD0B0648AF006A2BD" });

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
                .Returns(GetUserPermits(OkResponse));
            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored));
            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetHoldingDetails(OkResponse));
            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                            .Returns([new ProductKeyServiceResponse { ProductName = "test101", Edition = "1", Key = "123456" }]);
            A.CallTo(() => _fakeIs100Crypt.GetDecryptedHardwareIdFromUserPermit(A<UserPermitServiceResponse>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());
            A.CallTo(() => _fakePermitReaderWriter.ReadPermit(A<Permit>.Ignored)).Returns("fakepermit");

            var result = await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            result.Should().Be(HttpStatusCode.OK);

            A.CallTo(() => _fakePermitReaderWriter.WritePermit(A<string>.Ignored)).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit completed"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.XmlSerializationStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization started"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.XmlSerializationEnd.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization completed"
            ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.FileCreationEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml file created"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Error
           && call.GetArgument<EventId>(1) == EventIds.EmptyPermitXml.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Empty permit xml is received"
           ).MustNotHaveHappened();
        }

        [Test]
        public async Task WhenEmptyPermitXml_ThenFileIsNotCreated()
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetUserPermits(OkResponse));
            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored));
            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetHoldingDetails(OkResponse));
            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                         .Returns([new ProductKeyServiceResponse { ProductName = "test101", Edition = "1", Key = "123456" }]);
            A.CallTo(() => _fakeIs100Crypt.GetDecryptedHardwareIdFromUserPermit(A<UserPermitServiceResponse>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());
            A.CallTo(() => _fakePermitReaderWriter.ReadPermit(A<Permit>.Ignored)).Returns("");

            await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            A.CallTo(() => _fakePermitReaderWriter.WritePermit(A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit completed"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.XmlSerializationStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization started"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.XmlSerializationEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml serialization completed"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Error
           && call.GetArgument<EventId>(1) == EventIds.EmptyPermitXml.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Empty permit xml is received"
           ).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.FileCreationEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Xml file created"
           ).MustNotHaveHappened();
        }

        [Test]
        [TestCase(NoContent)]
        [TestCase("")]
        public async Task WhenHoldingServiceHasEmptyResponse_ThenPermitServiceReturnsNoContentResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetUserPermits(OkResponse));
            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetHoldingDetails(responseType));

            var result = await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            result.Should().Be(HttpStatusCode.NoContent);

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestCompletedWithNoContent.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!
                    .ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to HoldingsService responded with empty response"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(NoContent)]
        [TestCase("")]
        public async Task WhenUserPermitServiceHasEmptyResponse_ThenPermitServiceReturnsNoContentResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetUserPermits(responseType));

            var result = await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            result.Should().Be(HttpStatusCode.NoContent);

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestCompletedWithNoContent.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!
                .ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService responded with empty response"
            ).MustHaveHappenedOnceExactly();
        }

        private static List<HoldingsServiceResponse> GetHoldingDetails(string responseType)
        {
            switch(responseType)
            {
                case OkResponse:
                    return
                    [
                        new HoldingsServiceResponse
                        {
                            ProductTitle = "ProductTitle",
                            ProductCode = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle",
                                    CellCode = "CellCode",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        }
                    ];

                case NoContent:
                    return
                    [
                    ];

                default:
                    return null;
            }
        }

        private static UserPermitServiceResponse GetUserPermits(string responseType)
        {
            switch(responseType)
            {
                case OkResponse:
                    return new UserPermitServiceResponse
                    {
                        LicenceId = 1,
                        UserPermits = [new UserPermit { Title = "Title", Upn = "Upn" }]
                    };

                case NoContent:
                    return new UserPermitServiceResponse();

                default:
                    return null;
            }
        }

        private static List<UpnInfo> GetUpnInfoWithDecryptedHardwareId()
        {
            return
            [
                new UpnInfo()
                {
                    DecryptedHardwareId = "86C520323CEA3056B5ED7000F98814CB",
                    Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                },
                new UpnInfo()
                {
                    DecryptedHardwareId = "B2C0F91ADAAEA51CC5FCCA05C47499E4",
                    Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                }
            ];
        }
    }
}