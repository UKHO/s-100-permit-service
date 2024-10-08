using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Net;
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
        private ILogger<ProductKeyServiceApiClient> _fakeLogger;
        private IHttpClientFactory _fakeHttpClientFactory;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        private IProductKeyServiceApiClient? _productKeyServiceApiClient;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<ProductKeyServiceApiClient>>();
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
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

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _productKeyServiceApiClient = new ProductKeyServiceApiClient(_fakeLogger, _fakeHttpClientFactory);

            var result = _productKeyServiceApiClient.GetProductKeysAsync("http://test.com", productKeyServiceRequestData, "testToken", CancellationToken.None, _fakeCorrelationId);

            var deSerializedResult = JsonSerializer.Deserialize<List<ProductKeyServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deSerializedResult!.Count.Should().BeGreaterThanOrEqualTo(1);
            deSerializedResult![0].Key.Should().Be("123456");
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.Unauthorized)]
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

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _productKeyServiceApiClient = new ProductKeyServiceApiClient(_fakeLogger, _fakeHttpClientFactory);

            var result = _productKeyServiceApiClient.GetProductKeysAsync("http://test.com", new List<ProductKeyServiceRequest>() { }, "", CancellationToken.None, _fakeCorrelationId);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.MissingAccessToken.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Access token is empty or null"
            ).MustHaveHappenedOnceExactly();

            result.Result.StatusCode.Should().Be(httpStatusCode);
        }

        [Test]
        public void WhenNullAccessTokenIsPassed_ThenResponseShouldBeUnauthorized()
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonSerializer.Serialize(new List<ProductKeyServiceResponse>() { new() }), HttpStatusCode.Unauthorized);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _productKeyServiceApiClient = new ProductKeyServiceApiClient(_fakeLogger, _fakeHttpClientFactory);

            var result = _productKeyServiceApiClient.GetProductKeysAsync("http://test.com", new List<ProductKeyServiceRequest>() { }, null, CancellationToken.None, _fakeCorrelationId);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.MissingAccessToken.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Access token is empty or null"
            ).MustHaveHappenedOnceExactly();

            result.Result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}