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
                .Returns(GetServiceResponse<UserPermitServiceResponse>(HttpStatusCode.OK));

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetServiceResponse<List<HoldingsServiceResponse>>(HttpStatusCode.OK));

            A.CallTo(() => _fakeHoldingsService.FilterHoldingsByLatestExpiry(A<List<HoldingsServiceResponse>>.Ignored))
                .Returns(GetFilteredHoldingDetails());

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                                            .Returns(GetServiceResponse<List<ProductKeyServiceResponse>>(HttpStatusCode.OK));

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedKeysFromProductKeysAsync(A<List<ProductKeyServiceResponse>>.Ignored, A<string>.Ignored))
                .Returns(GetDecryptedKeysFromProductKeys());

            A.CallTo(() => _fakeIs100Crypt.GetDecryptedHardwareIdFromUserPermitAsync(A<UserPermitServiceResponse>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());

            A.CallTo(() => _fakePermitReaderWriter.ReadXsdVersion()).Returns("5.2.0");

            A.CallTo(() => _fakeIs100Crypt.CreateEncryptedKeyAsync(A<string>.Ignored, A<string>.Ignored)).Returns("123456");

            A.CallTo(() => _fakePermitReaderWriter.CreatePermitZipAsync(A<Dictionary<string, Permit>>.Ignored)).Returns(expectedStream);

            var response = await _permitService.ProcessPermitRequestAsync(1, _fakeCorrelationId, CancellationToken.None);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Value.Length.Should().Be(expectedStream.Length);

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

        [TestCase(HttpStatusCode.NoContent)]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        public async Task WhenHoldingServiceReturnsNotOkResponse_ThenPermitServiceReturnsNotOkResponse(HttpStatusCode httpStatusCode)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetServiceResponse<UserPermitServiceResponse>(HttpStatusCode.OK));

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetServiceResponse<List<HoldingsServiceResponse>>(httpStatusCode));

            var response = await _permitService.ProcessPermitRequestAsync(1, _fakeCorrelationId, CancellationToken.None);

            switch(response.StatusCode)
            {
                case HttpStatusCode.NoContent:
                    response.StatusCode.Should().Be(httpStatusCode);
                    break;

                case HttpStatusCode.BadRequest:
                    response.StatusCode.Should().Be(httpStatusCode);
                    response.ErrorResponse.Should().BeEquivalentTo(new
                    {
                        Errors = new List<ErrorDetail>
                    {
                        new() { Description = "Invalid licenceId", Source = "licenceId" }
                    }
                    });
                    break;

                case HttpStatusCode.NotFound:
                    response.StatusCode.Should().Be(httpStatusCode);
                    response.ErrorResponse.Should().BeEquivalentTo(new
                    {
                        Errors = new List<ErrorDetail>
                    {
                        new() { Description = "Licence not found", Source = "licenceId" }
                    }
                    });
                    break;
            }
            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
            ).MustHaveHappenedOnceExactly();
        }

        [TestCase(HttpStatusCode.NoContent)]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        public async Task WhenUserPermitServiceReturnsNotOkResponse_ThenPermitServiceReturnsNotOkResponse(HttpStatusCode httpStatusCode)
        {
            A.CallTo(() => _fakeUserPermitService.GetUserPermitAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetServiceResponse<UserPermitServiceResponse>(httpStatusCode));

            var response = await _permitService.ProcessPermitRequestAsync(1, _fakeCorrelationId, CancellationToken.None);

            switch(response.StatusCode)
            {
                case HttpStatusCode.NoContent:
                    response.StatusCode.Should().Be(httpStatusCode);
                    break;

                case HttpStatusCode.BadRequest:
                    response.StatusCode.Should().Be(httpStatusCode);
                    response.ErrorResponse.Should().BeEquivalentTo(new
                    {
                        Errors = new List<ErrorDetail>
                    {
                        new() { Description = "Invalid licenceId", Source = "licenceId" }
                    }
                    });
                    break;

                case HttpStatusCode.NotFound:
                    response.StatusCode.Should().Be(httpStatusCode);
                    response.ErrorResponse.Should().BeEquivalentTo(new
                    {
                        Errors = new List<ErrorDetail>
                    {
                        new() { Description = "Licence not found", Source = "licenceId" }
                    }
                    });
                    break;
            }

            A.CallTo(() => _fakeHoldingsService.GetHoldingsAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started."
            ).MustHaveHappenedOnceExactly();
        }

        private static ServiceResponseResult<T> GetServiceResponse<T>(HttpStatusCode httpStatusCode) where T : class, new()
        {
            switch(httpStatusCode)
            {
                case HttpStatusCode.OK:

                    var response = new T();

                    if(response is UserPermitServiceResponse userPermitServiceResponse)
                    {
                        userPermitServiceResponse.LicenceId = 1;
                        userPermitServiceResponse.UserPermits = [new UserPermit { Title = "Title", Upn = "Upn" }];
                    }
                    else if(response is List<ProductKeyServiceResponse> productKeyServiceResponse)
                    {
                        productKeyServiceResponse = [new() { ProductName = "CellCode", Edition = "1", Key = "123456" },
                            new() { ProductName = "CellCode1", Edition = "2", Key = "7891011" }];
                    }

                    else if(response is List<HoldingsServiceResponse> holdingsServiceResponse)
                    {
                        holdingsServiceResponse =
                        [
                            new()
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
                            new()
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
                            new()
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
                        ];
                    };

                    return ServiceResponseResult<T>.Success(response);

                case HttpStatusCode.NoContent:
                    return ServiceResponseResult<T>.NoContent();

                case HttpStatusCode.NotFound:
                    return ServiceResponseResult<T>.NotFound(new ErrorResponse() { CorrelationId = Guid.NewGuid().ToString(), Errors = [new ErrorDetail() { Description = "Licence not found", Source = "licenceId" }] });

                default: //BadRequest
                    return ServiceResponseResult<T>.BadRequest(new ErrorResponse() { CorrelationId = Guid.NewGuid().ToString(), Errors = [new ErrorDetail() { Description = "Invalid licenceId", Source = "licenceId" }] });
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