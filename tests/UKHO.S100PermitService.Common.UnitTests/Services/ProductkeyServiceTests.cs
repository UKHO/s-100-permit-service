using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
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
        private IProductKeyServiceAuthTokenProvider _fakeProductKeyServiceAuthTokenProvider;
        private IProductkeyServiceApiClient _fakeProductkeyServiceApiClient;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        private const string RequestError = "{\"correlationId\":\"fc771eb4-926b-4965-8de9-8b37288d3bd0\",\"errors\":[{\"source\":\"GetProductKey\",\"description\":\"Key not found for ProductName: test101 and Edition: 1.\"}]}";

        private IProductkeyService _productkeyService;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<ProductkeyService>>();
            _fakeProductkeyServiceApiConfiguration = Options.Create(new ProductkeyServiceApiConfiguration() { ClientId = "ClientId2", BaseUrl = "http://localhost:5000" });
            _fakeProductKeyServiceAuthTokenProvider = A.Fake<IProductKeyServiceAuthTokenProvider>();
            _fakeProductkeyServiceApiClient = A.Fake<IProductkeyServiceApiClient>();

            _productkeyService = new ProductkeyService(_fakeLogger, _fakeProductkeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, _fakeProductkeyServiceApiClient);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullProductkeyServiceLogger = () => new ProductkeyService(null, _fakeProductkeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, _fakeProductkeyServiceApiClient);
            nullProductkeyServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullProductkeyServiceApiConfiguration = () => new ProductkeyService(_fakeLogger, null, _fakeProductKeyServiceAuthTokenProvider, _fakeProductkeyServiceApiClient);
            nullProductkeyServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productkeyServiceApiConfiguration");

            Action nullProductKeyServiceAuthTokenProvider = () => new ProductkeyService(_fakeLogger, _fakeProductkeyServiceApiConfiguration, null, _fakeProductkeyServiceApiClient);
            nullProductKeyServiceAuthTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyServiceAuthTokenProvider");

            Action nullProductkeyServiceApiClient = () => new ProductkeyService(_fakeLogger, _fakeProductkeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, null);
            nullProductkeyServiceApiClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productkeyServiceApiClient");
        }

        [Test]
        public async Task WhenRequestsValidData_ThenProductKeyServiceReturnsValidResponse()
        {
            A.CallTo(() => _fakeProductkeyServiceApiClient.CallProductkeyServiceApiAsync
                    (A<string>.Ignored, A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                            .Returns(new HttpResponseMessage()
                            {
                                StatusCode = HttpStatusCode.OK,
                                RequestMessage = new HttpRequestMessage()
                                {
                                    RequestUri = new Uri("http://test.com")
                                },
                                Content = new StringContent(JsonConvert.SerializeObject(new List<ProductKeyServiceResponse>() { new() { ProductName = "test101", Edition = "1", Key = "123456" } }))
                            });

            var response = await _productkeyService.PostProductKeyServiceRequest([new() { ProductName = "test101", Edition = "1" }], _fakeCorrelationId);
            response.Count.Should().BeGreaterThanOrEqualTo(1);
            response.Equals(new List<ProductKeyServiceResponse>() { new() { ProductName = "test101", Edition = "1", Key = "123456" } });

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProductKeyServicePostPermitKeyRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProductKeyServicePostPermitKeyRequestCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} completed. | StatusCode : {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        public async Task WhenRequestsInvalidOrNonExistentData_ThenThrowException(HttpStatusCode httpStatusCode)
        {
            A.CallTo(() => _fakeProductkeyServiceApiClient.CallProductkeyServiceApiAsync
                    (A<string>.Ignored, A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                            .Returns(new HttpResponseMessage()
                            {
                                StatusCode = httpStatusCode,
                                RequestMessage = new HttpRequestMessage()
                                {
                                    RequestUri = new Uri("http://test.com")
                                },
                                Content = new StringContent(RequestError)
                            });

            await FluentActions.Invoking(async () => await _productkeyService.PostProductKeyServiceRequest([], _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to ProductKeyService POST Uri : {0} failed. | StatusCode : {1} | Error Details : {2}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProductKeyServicePostPermitKeyRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        [TestCase(HttpStatusCode.UnsupportedMediaType, "UnsupportedMediaType")]
        public async Task WhenProductKeyServiceResponseOtherThanOk_ThenThrowException(HttpStatusCode httpStatusCode, string content)
        {
            A.CallTo(() => _fakeProductkeyServiceApiClient.CallProductkeyServiceApiAsync
                    (A<string>.Ignored, A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                                .Returns(new HttpResponseMessage()
                                {
                                    StatusCode = httpStatusCode,
                                    RequestMessage = new HttpRequestMessage()
                                    {
                                        RequestUri = new Uri("http://test.com")
                                    },
                                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                                });

            await FluentActions.Invoking(async () => await _productkeyService.PostProductKeyServiceRequest([], _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to ProductKeyService POST Uri : {0} failed. | StatusCode : {1}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProductKeyServicePostPermitKeyRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();
        }
    }
}