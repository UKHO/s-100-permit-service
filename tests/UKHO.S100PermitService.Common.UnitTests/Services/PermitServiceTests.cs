using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class PermitServiceTests
    {
        private ILogger<PermitService> _fakeLogger;
        private IPermitReaderWriter _fakePermitReaderWriter;
        private IProductKeyService _fakeProductKeyService;
        private IS100Crypt _fakeS100Crypt;
        private IOptions<ProductKeyServiceApiConfiguration> _fakeProductKeyServiceApiConfiguration;
        private IOptions<PermitFileConfiguration> _fakePermitFileConfiguration;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        private const string PRODUCT_TYPE = "s100";
        private IPermitService _permitService;
        private IPermitRequestValidator _fakePermitRequestValidator;

        [SetUp]
        public void Setup()
        {
            _fakePermitReaderWriter = A.Fake<IPermitReaderWriter>();
            _fakeLogger = A.Fake<ILogger<PermitService>>();
            _fakeProductKeyService = A.Fake<IProductKeyService>();
            _fakeS100Crypt = A.Fake<IS100Crypt>();
            _fakeProductKeyServiceApiConfiguration = Options.Create(new ProductKeyServiceApiConfiguration() { HardwareId = "FAKE583E6CB6F32FD0B0648AF006A2BD" });
            _fakePermitFileConfiguration = A.Fake<IOptions<PermitFileConfiguration>>();

            _fakePermitRequestValidator = A.Fake<IPermitRequestValidator>();
            _permitService = new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService,
                                                _fakeS100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration, _fakePermitRequestValidator);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullPermitReaderWriter = () => new PermitService(null, _fakeLogger, _fakeProductKeyService, _fakeS100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration, _fakePermitRequestValidator);
            nullPermitReaderWriter.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitReaderWriter");

            Action nullLogger = () => new PermitService(_fakePermitReaderWriter, null, _fakeProductKeyService, _fakeS100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration, _fakePermitRequestValidator);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullProductKeyService = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, null, _fakeS100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration, _fakePermitRequestValidator);
            nullProductKeyService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyService");

            Action nullIs100Crypt = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService, null, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration, _fakePermitRequestValidator);
            nullIs100Crypt.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("s100Crypt");

            Action nullProductKeyServiceApiConfiguration = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService, _fakeS100Crypt, null, _fakePermitFileConfiguration, _fakePermitRequestValidator);
            nullProductKeyServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyServiceApiConfiguration");

            Action nullPermitFileConfiguration = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService, _fakeS100Crypt, _fakeProductKeyServiceApiConfiguration, null, _fakePermitRequestValidator);
            nullPermitFileConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitFileConfiguration");

            Action nullPermitRequestValidator = () => new PermitService(_fakePermitReaderWriter, _fakeLogger, _fakeProductKeyService, _fakeS100Crypt, _fakeProductKeyServiceApiConfiguration, _fakePermitFileConfiguration, null);
            nullPermitRequestValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitRequestValidator");
        }

        [Test]
        public async Task WhenPermitXmlHasValue_ThenFileIsCreated()
        {
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(GetExpectedXmlString()));
            var permitRequest = GetPermitRequests();

            A.CallTo(() => _fakePermitRequestValidator.Validate(A<PermitRequest>.Ignored)).Returns(new ValidationResult());

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<IEnumerable<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(ServiceResponseResult<IEnumerable<ProductKeyServiceResponse>>.Success(
                    new List<ProductKeyServiceResponse>
                    {
                        new() { ProductName = "CellCode", Edition = "1", Key = "123456" },
                        new() { ProductName = "CellCode1", Edition = "2", Key = "7891011" }
                    })));

            A.CallTo(() => _fakeS100Crypt.GetDecryptedKeysFromProductKeysAsync(A<IEnumerable<ProductKeyServiceResponse>>.Ignored, A<string>.Ignored))
                .Returns(GetDecryptedKeysFromProductKeys());

            A.CallTo(() => _fakeS100Crypt.GetDecryptedHardwareIdFromUserPermitAsync(A<IEnumerable<UserPermit>>.Ignored))
                .Returns(GetUpnInfoWithDecryptedHardwareId());

            A.CallTo(() => _fakePermitReaderWriter.ReadXsdVersion()).Returns("5.2.0");

            A.CallTo(() => _fakeS100Crypt.CreateEncryptedKeyAsync(A<string>.Ignored, A<string>.Ignored)).Returns("123456").Twice().Then.Returns("7891011");

            A.CallTo(() => _fakePermitReaderWriter.CreatePermitZipAsync(A<Dictionary<string, Permit>>.Ignored)).Returns(expectedStream);

            var response = await _permitService.ProcessPermitRequestAsync(PRODUCT_TYPE, GetPermitRequestDetails(), _fakeCorrelationId, CancellationToken.None);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Value.Length.Should().Be(expectedStream.Length);
            response.Origin.Should().BeNull();

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started for ProductType {productType}."
             ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log" &&
                call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProductsFilteredCellCount.ToEventId() &&
                call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Filtered products: Total count before filtering: {TotalCellCount}, after filtering for highest expiry dates and removing duplicates: {FilteredCellCount}."
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
            A.CallTo(() => _fakePermitRequestValidator.Validate(A<PermitRequest>.Ignored)).Returns(new ValidationResult());

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<IEnumerable<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<IEnumerable<ProductKeyServiceResponse>>.Failure(httpStatusCode, PermitServiceConstants.ProductKeyService));

            var response = await _permitService.ProcessPermitRequestAsync(PRODUCT_TYPE, GetPermitRequestDetails(), _fakeCorrelationId, CancellationToken.None);

            response.StatusCode.Should().Be(httpStatusCode);
            response.Origin.Should().Be(PermitServiceConstants.ProductKeyService);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log" &&
                call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId() &&
                call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started for ProductType {productType}."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log" &&
                call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProductsFilteredCellCount.ToEventId() &&
                call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Filtered products: Total count before filtering: {TotalCellCount}, after filtering for highest expiry dates and removing duplicates: {FilteredCellCount}."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log" &&
                call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId() &&
                call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed for ProductType {productType}."
            ).MustNotHaveHappened();
        }

        [Test]
        public async Task WhenPermitRequestValidationFailed_ThenPermitServiceReturnsBadRequestResponse()
        {
            var permitRequest = GetInvalidPermitRequestData();
            A.CallTo(() => _fakePermitRequestValidator.Validate(A<PermitRequest>.Ignored)).Returns(GetInvalidValidationResultForPermitRequest());

            var response = await _permitService.ProcessPermitRequestAsync(PRODUCT_TYPE, permitRequest, _fakeCorrelationId, CancellationToken.None);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Origin.Should().Be(PermitServiceConstants.PermitService);

            var errors = response.ErrorResponse.Errors.ToList();
            errors.Should().ContainSingle(e => e.Source.Equals("Product[0].PermitExpiryDate"));
            errors.Should().ContainSingle(e => e.Description.Equals("Must be today or a future date."));
            errors.Should().ContainSingle(e => e.Source.Equals("UserPermit[1].Upn"));
            errors.Should().ContainSingle(e => e.Description.Equals("Invalid UPN found for: FakeTitle2. UPN must be 46 characters long"));

            A.CallTo(() => _fakeProductKeyService.GetProductKeysAsync(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request started for ProductType {productType}."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.PermitRequestValidationFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit request validation failed for ProductType {productType}. Error Details: {errorMessage}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log" &&
                call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProductsFilteredCellCount.ToEventId() &&
                call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Filtered products: Total count before filtering: {TotalCellCount}, after filtering for highest expiry dates and removing duplicates: {FilteredCellCount}."
            ).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log" &&
                call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.ProcessPermitRequestCompleted.ToEventId() &&
                call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Process permit request completed for ProductType {productType}."
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
                        Title = "FakeTitle1",
                        Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                    },
                    new() {
                        Title = "FakeTitle2",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                    }
                }
            };
        }

        private static IEnumerable<UpnInfo> GetUpnInfoWithDecryptedHardwareId()
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
                    Edition = "2",
                    Key = "7891011",
                    DecryptedKey = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                }
            ];
        }

        private static PermitRequest GetPermitRequests()
        {
            return new PermitRequest()
            {
                Products =
                [
                    new Product()
                    {
                        ProductName = "CellCode",
                        EditionNumber = 1,
                        PermitExpiryDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-dd")
                    },
                    new Product()
                    {
                        ProductName = "CellCode1",
                        EditionNumber = 2,
                        PermitExpiryDate = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM-dd")
                    }
                ],
                UserPermits =
                [
                    new UserPermit()
                    {
                        Title = "FakeTitle1",
                        Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3"
                    },
                    new UserPermit()
                    {
                        Title = "FakeTitle2",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                    }
                ]
            };
        }

        private static PermitRequest GetInvalidPermitRequestData()
        {
            return new PermitRequest()
            {
                Products =
                [
                    new Product()
                    {
                        ProductName = "CellCode",
                        EditionNumber = 2,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
                    },
                    new Product()
                    {
                        ProductName = "CellCode1",
                        EditionNumber = 2,
                        PermitExpiryDate = DateTime.UtcNow.AddMonths(2).ToString("MM-dd-yyyy")
                    }
                ],
                UserPermits =
                [
                    new UserPermit()
                    {
                        Title = "FakeTitle1",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD44326"
                    },
                    new UserPermit()
                    {
                        Title = "FakeTitle2",
                        Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A145"
                    }
                ]
            };
        }

        private static string GetExpectedXmlString()
        {
            var sb = new StringBuilder();
            sb.Append("<?xmlversion=\"1.0\"encoding=\"UTF-8\"standalone=\"yes\"?><Permitxmlns:S100SE=\"http://www.iho.int/s100/se/5.2\"xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\"xmlns=\"http://www.iho.int/s100/se/5.2\"><S100SE:header>");
            sb.Append("<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName><S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier><S100SE:version>1</S100SE:version>");
            sb.Append("<S100SE:userpermit>fakeUserPermit</S100SE:userpermit></S100SE:header><S100SE:products><S100SE:productid=\"fakeID\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename><S100SE:editionNumber>1</S100SE:editionNumber>");
            sb.Append("<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:expiry>2024-09-02</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product></S100SE:products></Permit>");

            return sb.ToString();
        }

        private static ValidationResult GetInvalidValidationResultForPermitRequest()
        {
            return new ValidationResult(new List<ValidationFailure>
            {
                new("Product[0].PermitExpiryDate", "Must be today or a future date."),
                new("UserPermit[1].Upn", "Invalid UPN found for: FakeTitle2. UPN must be 46 characters long")
            });
        }
    }
}