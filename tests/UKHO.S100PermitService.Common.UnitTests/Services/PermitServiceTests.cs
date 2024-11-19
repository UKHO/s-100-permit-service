using FakeItEasy;
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
        const string BadRequest = "badRequest";

        private const string ErrorHoldingsNotFoundContent = "{\r\n  \"correlationId\": \"\",\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetHoldings\",\r\n      \"description\": \"Licence Not Found\"\r\n    }\r\n  ]\r\n}";
        private const string ErrorHoldingsBadRequestContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetHoldings\",\r\n      \"description\": \"Incorrect LicenceId\"\r\n    }\r\n  ]\r\n}";
        private const string ErrorUserPermitsNotFoundContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetUserPermits\",\r\n      \"description\": \"Licence Not Found\"\r\n    }\r\n  ]\r\n}";
        private const string ErrorUserPermitsBadRequestContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetUserPermits\",\r\n      \"description\": \"LicenceId is incorrect\"\r\n    }\r\n  ]\r\n}";

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

            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetUserPermits(OkResponse));

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetHoldingDetails(OkResponse));

            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored))
                .Returns(GetFilteredHoldingDetails());

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                                            .Returns([new ProductKeyServiceResponse { ProductName = "CellCode", Edition = "1", Key = "123456" }]);

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedKeysFromProductKeysAsync(A<List<ProductKeyServiceResponse>>.Ignored, A<string>.Ignored))
                .Returns(GetDecryptedKeysFromProductKeys());

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedHardwareIdFromUserPermitAsync(A<UserPermitServiceResponse>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());

            A.CallTo(() => _fakePermitReaderWriter.ReadXsdVersion()).Returns("5.2.0");

            A.CallTo(() => _fakeIs100Crypt.CreateEncryptedKeyAsync(A<string>.Ignored, A<string>.Ignored)).Returns("123456");

            A.CallTo(() => _fakePermitReaderWriter.CreatePermitZipAsync(A<Dictionary<string, Permit>>.Ignored)).Returns(expectedStream);

            var (httpResponseMessage, stream) = await _permitService.ProcessPermitRequestAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            stream.Length.Should().Be(expectedStream.Length);

            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored)).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
           ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed."
           ).MustHaveHappenedOnceExactly();
        }

        [TestCase(NoContent)]
        [TestCase("")]
        public async Task WhenHoldingServiceHasEmptyResponse_ThenPermitServiceReturnsNoContentResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetUserPermits(OkResponse));

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetHoldingDetails(responseType));

            var (httpResponseMessage, stream) = await _permitService.ProcessPermitRequestAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.NoContent);
            stream?.Equals(null);

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustHaveHappened();
            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
            ).MustHaveHappenedOnceExactly();
        }

        [TestCase(NoContent)]
        [TestCase("")]
        public async Task WhenUserPermitServiceHasEmptyResponse_ThenPermitServiceReturnsNoContentResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetUserPermits(responseType));

            var (httpResponseMessage, stream) = await _permitService.ProcessPermitRequestAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.NoContent);
            stream?.Equals(null);

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
            ).MustHaveHappenedOnceExactly();
        }

        [TestCase(NotFound)]
        public async Task WhenUserPermitServiceReturnsNotFoundResponse_ThenPermitServiceReturnsNotFoundResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetUserPermits(responseType));

            var (httpResponseMessage, stream) = await _permitService.ProcessPermitRequestAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
            httpResponseMessage.Content.ReadAsStringAsync().Result.Should().Be(ErrorUserPermitsNotFoundContent);
            stream.Length.Should().Be(0);

            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!
                .ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed."
            ).MustNotHaveHappened();
        }

        [TestCase(BadRequest)]
        public async Task WhenUserPermitServiceReturnsBadRequestResponse_ThenPermitServiceReturnsBadRequestResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetUserPermits(responseType));

            var (httpResponseMessage, stream) = await _permitService.ProcessPermitRequestAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            httpResponseMessage.Content.ReadAsStringAsync().Result.Should().Be(ErrorUserPermitsBadRequestContent);
            stream.Length.Should().Be(0);

            A.CallTo(() => _fakeUserPermitService.ValidateUpnsAndChecksum(A<UserPermitServiceResponse>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!
                    .ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed."
            ).MustNotHaveHappened();
        }

        [TestCase(NotFound)]
        public async Task WhenHoldingsServiceReturnsNotFoundResponse_ThenPermitServiceReturnsNotFoundResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetUserPermits(OkResponse));

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetHoldingDetails(responseType));

            var (httpResponseMessage, stream) = await _permitService.ProcessPermitRequestAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
            httpResponseMessage.Content.ReadAsStringAsync().Result.Should().Be(ErrorHoldingsNotFoundContent);
            stream.Should().NotBeNull();
            stream.Length.Should().Be(0);

            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId()
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!
               .ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed."
           ).MustNotHaveHappened();
        }

        [TestCase(BadRequest)]
        public async Task WhenHoldingsServiceReturnsBadRequestResponse_ThenPermitServiceReturnsBadRequestResponse(string responseType)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetUserPermits(OkResponse));

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetHoldingDetails(responseType));

            var (httpResponseMessage, stream) = await _permitService.ProcessPermitRequestAsync(1, CancellationToken.None, _fakeCorrelationId);

            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            httpResponseMessage.Content.ReadAsStringAsync().Result.Should().Be(ErrorHoldingsBadRequestContent);
            stream.Length.Should().Be(0);

            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!
                    .ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed."
            ).MustNotHaveHappened();
        }

        private static (HttpResponseMessage, List<HoldingsServiceResponse>?) GetHoldingDetails(string responseType)
        {
            switch(responseType)
            {
                case OkResponse:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK
                    },
                    [
                        new HoldingsServiceResponse
                        {
                            UnitTitle = "ProductTitle",
                            UnitName = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                },
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        },
                        new HoldingsServiceResponse
                        {
                            UnitTitle = "ProductTitle1",
                            UnitName = "ProductCode1",
                            ExpiryDate = DateTime.UtcNow.AddDays(4),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle1",
                                    DatasetName = "CellCode1",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        },
                        new HoldingsServiceResponse
                        {
                            UnitTitle = "ProductTitle",
                            UnitName = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(3),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        }
                    ]);

                case NoContent:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NoContent
                    }, []);

                case "":
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NoContent
                    }, null);

                case NotFound:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        Content = new StringContent(ErrorHoldingsNotFoundContent, Encoding.UTF8, "application/json")
                    }, null);

                case BadRequest:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        Content = new StringContent(ErrorHoldingsBadRequestContent, Encoding.UTF8, "application/json")
                    }, null);

                default:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NoContent
                    }, null);
            }
        }

        private static (HttpResponseMessage, UserPermitServiceResponse?) GetUserPermits(string responseType)
        {
            switch(responseType)
            {
                case OkResponse:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK
                    }, new UserPermitServiceResponse
                    {
                        LicenceId = 1,
                        UserPermits = [new UserPermit { Title = "Title", Upn = "Upn" }]
                    });

                case NoContent:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NoContent
                    }, new UserPermitServiceResponse());

                case "":
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NoContent
                    }, null);

                case NotFound:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        Content = new StringContent(ErrorUserPermitsNotFoundContent, Encoding.UTF8, "application/json")
                    }, null);

                case BadRequest:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        Content = new StringContent(ErrorUserPermitsBadRequestContent, Encoding.UTF8, "application/json")
                    }, null);

                default:
                    return (new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NoContent
                    }, null);
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
                    UnitTitle = "ProductTitle",
                    UnitName = "ProductCode",
                    ExpiryDate = DateTime.UtcNow.AddDays(5),
                    Datasets =
                    [
                        new Dataset
                        {
                            DatasetTitle = "CellTitle",
                            DatasetName = "CellCode",
                            LatestEditionNumber = 1,
                            LatestUpdateNumber = 1
                        }
                    ]
                },
                new HoldingsServiceResponse
                {
                    UnitTitle = "ProductTitle1",
                    UnitName = "ProductCode1",
                    ExpiryDate = DateTime.UtcNow.AddDays(4),
                    Datasets =
                    [
                        new Dataset
                        {
                            DatasetTitle = "CellTitle1",
                            DatasetName = "CellCode1",
                            LatestEditionNumber = 1,
                            LatestUpdateNumber = 1
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