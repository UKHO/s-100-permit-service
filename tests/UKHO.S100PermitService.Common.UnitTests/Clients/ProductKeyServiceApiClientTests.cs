using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.UnitTests.Handler;

namespace UKHO.S100PermitService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class ProductKeyServiceApiClientTests
    {
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private ILogger<ProductKeyServiceApiClient> _fakeLogger;
        private IHttpClientFactory _fakeHttpClientFactory;
        private IProductKeyServiceApiClient? _productKeyServiceApiClient;
        private HttpClient _fakeHttpClient;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<ProductKeyServiceApiClient>>();
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
            _fakeHttpClient = A.Fake<HttpClient>();
        }

        [TearDown]
        public void TearDown()
        {
            _fakeHttpClient.Dispose();
        }

        [Test]
        public void WhenValidDataIsPassed_ThenProductKeyServiceReturnsOkResponse()
        {
            var productKeyServiceRequestData = new List<ProductKeyServiceRequest>() { new() { ProductName = "test101", Edition = "1" } };

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                                    JsonSerializer.Serialize(new List<ProductKeyServiceResponse>()
                                    { new() { ProductName = "test101", Edition = "1", Key = "123456"} }), HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            _productKeyServiceApiClient = new ProductKeyServiceApiClient(_fakeLogger, httpClient);

            var result = _productKeyServiceApiClient.GetProductKeysAsync("http://test.com", productKeyServiceRequestData, "testToken", _fakeCorrelationId, CancellationToken.None);

            var deSerializedResult = JsonSerializer.Deserialize<List<ProductKeyServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deSerializedResult!.Count.Should().BeGreaterThanOrEqualTo(1);
            deSerializedResult![0].Key.Should().Be("123456");
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.ServiceUnavailable)]
        [TestCase(HttpStatusCode.UnsupportedMediaType)]
        public void WhenProductKeyServiceResponseOtherThanOk_ThenResponseShouldNotBeOk(HttpStatusCode httpStatusCode)
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonSerializer.Serialize(new List<ProductKeyServiceResponse>() { new() }), httpStatusCode);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            _productKeyServiceApiClient = new ProductKeyServiceApiClient(_fakeLogger, httpClient);

            var result = _productKeyServiceApiClient.GetProductKeysAsync("http://test.com", [], "fakeToken", _fakeCorrelationId, CancellationToken.None);

            result.Result.StatusCode.Should().Be(httpStatusCode);
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void WhenNullOrEmptyAccessTokenIsPassed_ThenResponseShouldBeUnauthorized(string? token)
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonSerializer.Serialize(new List<ProductKeyServiceResponse>() { new() }), HttpStatusCode.Unauthorized);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            _productKeyServiceApiClient = new ProductKeyServiceApiClient(_fakeLogger, httpClient);

            var result = _productKeyServiceApiClient.GetProductKeysAsync("http://test.com", [], token, _fakeCorrelationId, CancellationToken.None);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.MissingAccessToken.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Access token is empty or null."
            ).MustHaveHappenedOnceExactly();

            result.Result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public void WhenAccessTokenIsProvided_ThenShouldSetBearerTokenAndAddCorrelationIdHeader()
        {
            var httpRequestMessage = new HttpRequestMessage();
            var productKeyServiceRequestData = new List<ProductKeyServiceRequest>() { new() { ProductName = "test101", Edition = "1" } };

            A.CallTo(() => _fakeHttpClient.SendAsync(A<HttpRequestMessage>.Ignored, A<CancellationToken>.Ignored))
                .Invokes((HttpRequestMessage requestMessage, CancellationToken token) =>
                {
                    httpRequestMessage = requestMessage;
                })
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new List<ProductKeyServiceResponse>()
                                    { new() { ProductName = "test101", Edition = "1", Key = "123456"} }))
                }));

            _productKeyServiceApiClient = new ProductKeyServiceApiClient(_fakeLogger, _fakeHttpClient);

            var result = _productKeyServiceApiClient.GetProductKeysAsync("http://test.com", productKeyServiceRequestData, "testToken", _fakeCorrelationId, CancellationToken.None);

            var deSerializedResult = JsonSerializer.Deserialize<List<ProductKeyServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);
            httpRequestMessage.Headers.Authorization.Parameter.Should().Be("testToken");

            httpRequestMessage.Headers.Contains(PermitServiceConstants.XCorrelationIdHeaderKey).Should().BeTrue();
            httpRequestMessage.Headers.GetValues(PermitServiceConstants.XCorrelationIdHeaderKey).Should().Contain(_fakeCorrelationId);
            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deSerializedResult!.Count.Should().BeGreaterThanOrEqualTo(1);
            deSerializedResult![0].Key.Should().Be("123456");
        }
    }
}