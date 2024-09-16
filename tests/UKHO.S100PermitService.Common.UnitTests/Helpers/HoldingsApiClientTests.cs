using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.UnitTests.Handler;

namespace UKHO.S100PermitService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class HoldingsApiClientTests
    {
        private HoldingsApiClient? _holdingsApiClient;
        private IHttpClientFactory _fakeHttpClientFactory;

        [SetUp]
        public void Setup()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        private const string ResponseValid = "[\r\n  {\r\n    \"productCode\": \"P1231\",\r\n    \"productTitle\": \"P1231\",\r\n    \"expiryDate\": \"2026-01-31T23:59:00Z\",\r\n    \"cells\": [\r\n      {\r\n        \"cellCode\": \"1\",\r\n        \"cellTitle\": \"1\",\r\n        \"latestEditionNumber\": \"1\",\r\n        \"latestUpdateNumber\": \"11\"\r\n      }\r\n    ]\r\n  }\r\n]";

        [Test]
        public void WhenValidHoldingsServiceDataIsPassed_ThenReturnsOKResponse()
        {
            HttpMessageHandler messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                ResponseValid, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _holdingsApiClient = new HoldingsApiClient(_fakeHttpClientFactory);

            Task<HttpResponseMessage> result = _holdingsApiClient.GetHoldingsDataAsync("http://test.com", 1, "asdfsa");

            List<HoldingsServiceResponse> deSerializedResult = JsonConvert.DeserializeObject<List<HoldingsServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deSerializedResult.Count.Should().BeGreaterThanOrEqualTo(1);
        }

        [Test]
        public void WhenInvalidHoldingsServiceDataIsPassed_ThenReturnsUnauthorizedResponse()
        {
            HttpMessageHandler messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonConvert.SerializeObject(new List<HoldingsServiceResponse>() { new() }), HttpStatusCode.Unauthorized);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _holdingsApiClient = new HoldingsApiClient(_fakeHttpClientFactory);

            Task<HttpResponseMessage> result = _holdingsApiClient.GetHoldingsDataAsync("http://test.com", 8, null);

            result.Result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
