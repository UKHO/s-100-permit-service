using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models.ProductkeyService;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class ProductkeyServiceTests
    {
        private ILogger<ProductkeyService> _fakeLogger;
        private IOptions<ProductkeyServiceApiConfiguration> _fakeProductkeyServiceApiConfiguration;
        private IAuthProductKeyServiceTokenProvider _fakeAuthPksTokenProvider;
        private IProductkeyServiceApiClient _fakeProductkeyServiceApiClient;
        private IProductkeyService _productkeyService;
        private const string CorrelationId = "fc771eb4-926b-4965-8de9-8b37288d3bd0";
        private const string RequestError = "{\"correlationId\":\"fc771eb4-926b-4965-8de9-8b37288d3bd0\",\"errors\":[{\"source\":\"GetProductKey\",\"description\":\"Key not found for ProductName: test101 and Edition: 1.\"}]}";

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<ProductkeyService>>();
            _fakeProductkeyServiceApiConfiguration = Options.Create(new ProductkeyServiceApiConfiguration() { ClientId = "ClientId2", BaseUrl = "http://localhost:5000" });
            _fakeAuthPksTokenProvider = A.Fake<IAuthProductKeyServiceTokenProvider>();
            _fakeProductkeyServiceApiClient = A.Fake<IProductkeyServiceApiClient>();

            _productkeyService = new ProductkeyService(_fakeLogger, _fakeProductkeyServiceApiConfiguration, _fakeAuthPksTokenProvider, _fakeProductkeyServiceApiClient);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullProductkeyServiceLogger = () => new ProductkeyService(null, _fakeProductkeyServiceApiConfiguration, _fakeAuthPksTokenProvider, _fakeProductkeyServiceApiClient);
            nullProductkeyServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullProductkeyServiceApiConfiguration = () => new ProductkeyService(_fakeLogger, null, _fakeAuthPksTokenProvider, _fakeProductkeyServiceApiClient);
            nullProductkeyServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productkeyServiceApiConfiguration");

            Action nullAuthPksTokenProvider = () => new ProductkeyService(_fakeLogger, _fakeProductkeyServiceApiConfiguration, null, _fakeProductkeyServiceApiClient);
            nullAuthPksTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("authPksTokenProvider");

            Action nullProductkeyServiceApiClient = () => new ProductkeyService(_fakeLogger, _fakeProductkeyServiceApiConfiguration, _fakeAuthPksTokenProvider, null);
            nullProductkeyServiceApiClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productkeyServiceApiClient");
        }

        [Test]
        public async Task WhenRequestsValidData_ThenProductKeyServiceReturnsValidResponse()
        {
            A.CallTo(() => _fakeProductkeyServiceApiClient.GetPermitKeyAsync
                    (A<string>.Ignored, A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StringContent(JsonConvert.SerializeObject(new List<ProductKeyServiceResponse>() { new() { ProductName = "test101", Edition = "1", Key = "123456" } }))
                });

            var response = await _productkeyService.GetPermitKeyAsync([new() { ProductName = "test101", Edition = "1" }], CorrelationId);
            response.Count.Should().BeGreaterThanOrEqualTo(1);
            response.Equals(new List<ProductKeyServiceResponse>() { new() { ProductName = "test101", Edition = "1", Key = "123456" } });

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetPermitKeyStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get permit key from Product Key Service started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetPermitKeyCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get permit key from Product Key Service completed | StatusCode : {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        public async Task WhenRequestsInvalidValidData_ThenThrowException(HttpStatusCode httpStatusCode)
        {
            A.CallTo(() => _fakeProductkeyServiceApiClient.GetPermitKeyAsync
                    (A<string>.Ignored, A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = httpStatusCode,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StringContent(RequestError)
                });

            await FluentActions.Invoking(async () => await _productkeyService.GetPermitKeyAsync([], CorrelationId)).Should().ThrowAsync<Exception>();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetPermitKeyStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get permit key from Product Key Service started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.GetPermitKeyException.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to retrieve permit key for Product Key Service | StatusCode : {StatusCode}| Errors : {ErrorDetails}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        [TestCase(HttpStatusCode.UnsupportedMediaType, "UnsupportedMediaType")]
        public async Task WhenProductKeyServiceResponseOtherThanOk_ThenThrowException(HttpStatusCode httpStatusCode, string content)
        {
            A.CallTo(() => _fakeProductkeyServiceApiClient.GetPermitKeyAsync
                    (A<string>.Ignored, A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = httpStatusCode,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                });

            await FluentActions.Invoking(async () => await _productkeyService.GetPermitKeyAsync([], CorrelationId)).Should().ThrowAsync<Exception>();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetPermitKeyStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get permit key from Product Key Service started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.GetPermitKeyException.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to retrieve permit key for Product Key Service | StatusCode : {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }
    }
}