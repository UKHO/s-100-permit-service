using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.UnitTests.Handler;

namespace UKHO.S100PermitService.Common.UnitTests.Clients
{
    [TestFixture]
    public class HoldingsApiClientTests
    {
        private ILogger<HoldingsApiClient> _fakeLogger;
        private HoldingsApiClient? _holdingsApiClient;
        private IHttpClientFactory _fakeHttpClientFactory;
        private readonly string _correlationId = Guid.NewGuid().ToString();
        const string FakeUri = "http://test.com";

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<HoldingsApiClient>>();
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        private const string ResponseValid = "[\r\n  {\r\n    \"productCode\": \"P1231\",\r\n    \"productTitle\": \"P1231\",\r\n    \"expiryDate\": \"2026-01-31T23:59:00Z\",\r\n    \"cells\": [\r\n      {\r\n        \"cellCode\": \"1\",\r\n        \"cellTitle\": \"1\",\r\n        \"latestEditionNumber\": \"1\",\r\n        \"latestUpdateNumber\": \"11\"\r\n      }\r\n    ]\r\n  }\r\n]";

        [Test]
        public void WhenValidHoldingsServiceDataIsPassed_ThenReturnsOKResponse()
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                ResponseValid, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _holdingsApiClient = new HoldingsApiClient(_fakeLogger, _fakeHttpClientFactory);

            var result = _holdingsApiClient.GetHoldingsAsync(FakeUri, 1, "asdfsa", CancellationToken.None, _correlationId);

            var deserializedResult = JsonConvert.DeserializeObject<List<HoldingsServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deserializedResult.Count.Should().BeGreaterThanOrEqualTo(1);
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.ServiceUnavailable)]
        [TestCase(HttpStatusCode.UnsupportedMediaType)]
        public void WhenInvalidHoldingsServiceDataIsPassed_ThenResponseShouldNotBeOk(HttpStatusCode httpStatusCode)
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonConvert.SerializeObject(new List<HoldingsServiceResponse> { new() }), httpStatusCode);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri(FakeUri)
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _holdingsApiClient = new HoldingsApiClient(_fakeLogger, _fakeHttpClientFactory);

            var result = _holdingsApiClient.GetHoldingsAsync(FakeUri, 8, "", CancellationToken.None, _correlationId);

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
                JsonConvert.SerializeObject(new List<HoldingsServiceResponse> { new() }), HttpStatusCode.Unauthorized);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri(FakeUri)
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _holdingsApiClient = new HoldingsApiClient(_fakeLogger, _fakeHttpClientFactory);

            var result = _holdingsApiClient.GetHoldingsAsync(FakeUri, 8, null, CancellationToken.None, _correlationId);

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
