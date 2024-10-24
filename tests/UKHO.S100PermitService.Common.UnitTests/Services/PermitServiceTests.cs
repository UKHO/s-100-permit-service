﻿using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Holdings;
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
        private IOptions<PermitFileConfiguration> _fakePermitFileConfiguration;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        const string NoContent = "noContent";
        const string OkResponse = "okResponse";
        const string NotFound = "notFound";

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
            _fakePermitFileConfiguration = A.Fake<IOptions<PermitFileConfiguration>>();

            _permitService = new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService,
                                                _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullPermitReaderWriter = () => new PermitService(null, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullPermitReaderWriter.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitReaderWriter");

            Action nullLogger = () => new PermitService(_fakePermitReaderWriter, null, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullHoldingsService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, null, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullHoldingsService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsService");

            Action nullUserPermitService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, null, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullUserPermitService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitService");

            Action nullProductKeyService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, null, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullProductKeyService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyService");

            Action nullIs100Crypt = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, null, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration);
            nullIs100Crypt.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("s100Crypt");

            Action nullProductKeyServiceApiConfiguration = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, null, _fakePermitFileConfiguration);
            nullProductKeyServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyServiceApiConfiguration");

            Action nullPermitFileConfiguration = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeHoldingsService, _fakeUserPermitService, _fakeProductKeyService, _fakeIs100Crypt, _fakeProductKeyServiceApiConfiguration, null);
            nullPermitFileConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitFileConfiguration");
        }

        [Test]
        public async Task WhenPermitXmlHasValue_ThenFileIsCreated()
        {
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(GetExpectedXmlString()));

            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetUserPermits(OkResponse));

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetHoldingDetails(OkResponse));

            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored))
                .Returns(GetFilteredHoldingDetails());

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                            .Returns([new ProductKeyServiceResponse { ProductName = "CellCode", Edition = "1", Key = "123456" }]);

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedKeysFromProductKeys(A<List<ProductKeyServiceResponse>>.Ignored, A<string>.Ignored))
                .Returns(GetDecryptedKeysFromProductKeys());

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedHardwareIdFromUserPermit(A<UserPermitServiceResponse>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());

            A.CallTo(() => _fakePermitReaderWriter.ReadXsdVersion()).Returns("5.2.0");

            A.CallTo(() => _fakeIs100Crypt.CreateEncryptedKey(A<string>.Ignored, A<string>.Ignored)).Returns("123456");

            A.CallTo(() => _fakePermitReaderWriter.CreatePermits(A<Dictionary<string, Permit>>.Ignored)).Returns(expectedStream);

            var (httpStatusCode, stream) = await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpStatusCode.Should().Be(HttpStatusCode.OK);
            stream.Length.Should().Be(expectedStream.Length);

            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored)).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.FileCreationStart.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml file creation started"
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.GetProductListStarted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get Product List details from HoldingServiceResponse and ProductKeyService started for Title: {title}"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.GetProductListCompleted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get Product List from HoldingServiceResponse and ProductKeyService completed for title : {title}"
           ).MustHaveHappenedTwiceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.FileCreationEnd.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit Xml file creation completed"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.CreatePermitEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit completed"
           ).MustHaveHappenedOnceExactly();
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

            var (httpStatusCode, stream) = await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpStatusCode.Should().Be(HttpStatusCode.NoContent);

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustHaveHappened();
            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored)).MustNotHaveHappened();

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
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to HoldingsService responded with empty response"
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

            result.httpStatusCode.Should().Be(HttpStatusCode.NoContent);

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored)).MustNotHaveHappened();

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

        [Test]
        [TestCase(NotFound)]
        public async Task WhenUserPermitServiceReturnsNotFoundResponse_ThenPermitServiceReturnsNotFoundResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetUserPermits(responseType));

            var result = await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            result.httpStatusCode.Should().Be(HttpStatusCode.NotFound);
            result.stream.Length.Should().Be(0);

            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.CreatePermitEnd.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!
                .ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit completed"
            ).MustNotHaveHappened();
        }

        [Test]
        [TestCase(NotFound)]
        public async Task WhenHoldingsServiceReturnsNotFoundResponse_ThenPermitServiceReturnsNotFoundResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetUserPermits(OkResponse));

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(GetHoldingDetails(responseType));

            var result = await _permitService.CreatePermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            result.httpStatusCode.Should().Be(HttpStatusCode.NotFound);
            result.stream.Length.Should().Be(0);

            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.CreatePermitStart.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<EventId>(1) == EventIds.CreatePermitEnd.ToEventId()
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!
               .ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "CreatePermit completed"
           ).MustNotHaveHappened();
        }

        private static (HttpStatusCode, List<HoldingsServiceResponse>?) GetHoldingDetails(string responseType)
        {
            switch(responseType)
            {
                case OkResponse:
                    return (HttpStatusCode.OK,
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
                                },
                                new Cell
                                {
                                    CellTitle = "CellTitle",
                                    CellCode = "CellCode",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        },
                        new HoldingsServiceResponse
                        {
                            ProductTitle = "ProductTitle1",
                            ProductCode = "ProductCode1",
                            ExpiryDate = DateTime.UtcNow.AddDays(4),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle1",
                                    CellCode = "CellCode1",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        },
                        new HoldingsServiceResponse
                        {
                            ProductTitle = "ProductTitle",
                            ProductCode = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(3),
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
                    ]);

                case NoContent:
                    return (HttpStatusCode.NoContent,
                    [
                    ]);

                case NotFound:
                    return (HttpStatusCode.NotFound, null);

                default:
                    return (HttpStatusCode.NoContent,
                    [
                    ]);
            }
        }

        private static (HttpStatusCode, UserPermitServiceResponse?) GetUserPermits(string responseType)
        {
            switch(responseType)
            {
                case OkResponse:
                    return (HttpStatusCode.OK, new UserPermitServiceResponse
                    {
                        LicenceId = 1,
                        UserPermits = [new UserPermit { Title = "Title", Upn = "Upn" }]
                    });

                case NoContent:
                    return (HttpStatusCode.NoContent, new UserPermitServiceResponse());

                case NotFound:
                    return (HttpStatusCode.NotFound, null);

                default:
                    //return null;
                    return (HttpStatusCode.NoContent, new UserPermitServiceResponse());
            }
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

        private static List<HoldingsServiceResponse> GetFilteredHoldingDetails()
        {
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
                },
                new HoldingsServiceResponse
                {
                    ProductTitle = "ProductTitle1",
                    ProductCode = "ProductCode1",
                    ExpiryDate = DateTime.UtcNow.AddDays(4),
                    Cells =
                    [
                        new Cell
                        {
                            CellTitle = "CellTitle1",
                            CellCode = "CellCode1",
                            LatestEditionNumber = "1",
                            LatestUpdateNumber = "1"
                        }
                    ]
                }
            ];
        }

        private string GetExpectedXmlString()
        {
            var expectedResult = "<?xmlversion=\"1.0\"encoding=\"UTF-8\"standalone=\"yes\"?><Permitxmlns:S100SE=\"http://www.iho.int/s100/se/5.2\"xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\"xmlns=\"http://www.iho.int/s100/se/5.2\"><S100SE:header>";
            expectedResult += "<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName><S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier><S100SE:version>1</S100SE:version>";
            expectedResult += "<S100SE:userpermit>fakeUserPermit</S100SE:userpermit></S100SE:header><S100SE:products><S100SE:productid=\"fakeID\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename><S100SE:editionNumber>1</S100SE:editionNumber>";
            expectedResult += "<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:expiry>2024-09-02</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product></S100SE:products></Permit>";

            return expectedResult;
        }
    }
}