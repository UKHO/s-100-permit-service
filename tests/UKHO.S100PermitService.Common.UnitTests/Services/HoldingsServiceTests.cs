using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class HoldingsServiceTests
    {
        private ILogger<HoldingsService> _fakeLogger;
        private IOptions<HoldingsServiceApiConfiguration> _fakeHoldingsServiceApiConfiguration;
        private IAuthHoldingsServiceTokenProvider _fakeAuthHoldingsServiceTokenProvider;
        private IHoldingsApiClient _fakeHoldingsApiClient;
        private IHoldingsService _holdingsService;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        [SetUp]
        public void Setup()
        {
            _fakeHoldingsServiceApiConfiguration = Options.Create(new HoldingsServiceApiConfiguration() { HoldingsClientId = "ClientId2" });
            _fakeHoldingsApiClient = A.Fake<IHoldingsApiClient>();
            _fakeAuthHoldingsServiceTokenProvider = A.Fake<IAuthHoldingsServiceTokenProvider>();
            _fakeLogger = A.Fake<ILogger<HoldingsService>>();

            _holdingsService = new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeAuthHoldingsServiceTokenProvider, _fakeHoldingsApiClient);
        }

        private const string ResponseValid = "[\r\n  {\r\n    \"productCode\": \"P1231\",\r\n    \"productTitle\": \"P1231\",\r\n    \"expiryDate\": \"2026-01-31T23:59:00Z\",\r\n    \"cells\": [\r\n      {\r\n        \"cellCode\": \"1\",\r\n        \"cellTitle\": \"1\",\r\n        \"latestEditionNumber\": \"1\",\r\n        \"latestUpdateNumber\": \"11\"\r\n      }\r\n    ]\r\n  }\r\n]";

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
             () => new HoldingsService(null, _fakeHoldingsServiceApiConfiguration, _fakeAuthHoldingsServiceTokenProvider, _fakeHoldingsApiClient))
             .ParamName
             .Should().Be("logger");

            Assert.Throws<ArgumentNullException>(
             () => new HoldingsService(_fakeLogger, null, _fakeAuthHoldingsServiceTokenProvider, _fakeHoldingsApiClient))
             .ParamName
             .Should().Be("holdingsApiConfiguration");

            Assert.Throws<ArgumentNullException>(
             () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, null, _fakeHoldingsApiClient))
             .ParamName
             .Should().Be("authHoldingsServiceTokenProvider");

            Assert.Throws<ArgumentNullException>(
             () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeAuthHoldingsServiceTokenProvider, null))
             .ParamName
             .Should().Be("holdingsApiClient");
        }

        [Test]
        public async Task WhenHoldingsServiceRequestsValidData_ThenReturnHoldingsServiceValidResponse()
        {
            A.CallTo(() => _fakeHoldingsApiClient.GetHoldingsDataAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://localhost:5000/holdings/1/s100")
                    },
                    Content = new StringContent(ResponseValid)
                });

            List<HoldingsServiceResponse> response = await _holdingsService.GetHoldings(1, _fakeCorrelationId);
            response.Count.Should().BeGreaterThanOrEqualTo(1);

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.GetHoldingsDataToHoldingsService.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get holdings data to Holdings Service started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.GetHoldingsDataToHoldingsCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get holdings data to Holdings Service completed | StatusCode : {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenHoldingsServiceRequestsInvalidData_ThenReturnsException()
        {
            A.CallTo(() => _fakeHoldingsApiClient.GetHoldingsDataAsync
                     (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.BadRequest,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://localhost:5000/holdings/test/s100")
                     },
                     Content = new StringContent("Bad Request", Encoding.UTF8, "application/json")
                 });

            Assert.ThrowsAsync<Exception>(() => _holdingsService.GetHoldings(9, _fakeCorrelationId));

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.GetHoldingsDataToHoldingsService.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get holdings data to Holdings Service started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.GetHoldingsDataToHoldingsFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to retrieve get holdings data with | StatusCode : {StatusCode}| Errors : {ErrorDetails} for Holdings Service."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public async Task WhenHoldingsServiceResponseOtherThanOkAndBadRequest_ThenReturnsException(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeHoldingsApiClient.GetHoldingsDataAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    RequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("http://localhost:5000/holdings/test/s100")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                });

            Assert.ThrowsAsync<Exception>(() => _holdingsService.GetHoldings(23, _fakeCorrelationId));

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.GetHoldingsDataToHoldingsService.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get holdings data to Holdings Service started."
              ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.GetHoldingsDataToHoldingsFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to get holdings data | StatusCode : {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }
    }
}
