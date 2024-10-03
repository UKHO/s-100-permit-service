using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class HoldingsServiceTests
    {
        private ILogger<HoldingsService> _fakeLogger;
        private IOptions<HoldingsServiceApiConfiguration> _fakeHoldingsServiceApiConfiguration;
        private IHoldingsServiceAuthTokenProvider _fakeHoldingsServiceAuthTokenProvider;
        private IHoldingsApiClient _fakeHoldingsApiClient;
        private IHoldingsService _holdingsService;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        const string FakeUri = "http://test.com";
        const string AccessToken = "access-token";

        private const string OkResponseContent = "[\r\n  {\r\n    \"productCode\": \"P1231\",\r\n    \"productTitle\": \"P1231\",\r\n    \"expiryDate\": \"2026-01-31T23:59:00Z\",\r\n    \"cells\": [\r\n      {\r\n        \"cellCode\": \"1\",\r\n        \"cellTitle\": \"1\",\r\n        \"latestEditionNumber\": \"1\",\r\n        \"latestUpdateNumber\": \"11\"\r\n      }\r\n    ]\r\n  }\r\n]";
        private const string ErrorBadRequestContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetHoldings\",\r\n      \"description\": \"Incorrect LicenceId\"\r\n    }\r\n  ]\r\n}";
        private const string ErrorNotFoundContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetHoldings\",\r\n      \"description\": \"Licence Not Found\"\r\n    }\r\n  ]\r\n}";

        [SetUp]
        public void Setup()
        {
            _fakeHoldingsServiceApiConfiguration = Options.Create(new HoldingsServiceApiConfiguration { ClientId = "ClientId2", BaseUrl = FakeUri, RequestTimeoutInMinutes = 5 });
            _fakeHoldingsApiClient = A.Fake<IHoldingsApiClient>();
            _fakeHoldingsServiceAuthTokenProvider = A.Fake<IHoldingsServiceAuthTokenProvider>();
            _fakeLogger = A.Fake<ILogger<HoldingsService>>();

            _holdingsService = new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullHoldingsLogger = () => new HoldingsService(null, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient);
            nullHoldingsLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullHoldingsApiConfiguration = () => new HoldingsService(_fakeLogger, null, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient);
            nullHoldingsApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsApiConfiguration");

            Action nullAuthHoldingsServiceTokenProvider = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, null, _fakeHoldingsApiClient);
            nullAuthHoldingsServiceTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsServiceAuthTokenProvider");

            Action nullHoldingsApiClient = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, null);
            nullHoldingsApiClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsApiClient");
        }

        [Test]
        public async Task WhenValidLicenceId_ThenHoldingsServiceReturns200OkResponse()
        {
            A.CallTo(() => _fakeHoldingsApiClient.GetHoldingsAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri(FakeUri)
                    },
                    Content = new StringContent(OkResponseContent)
                });
            A.CallTo(() => _fakeHoldingsServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            var response = await _holdingsService.GetHoldingsAsync(1, CancellationToken.None, _fakeCorrelationId);
            response.Count.Should().BeGreaterThanOrEqualTo(1);
            response.Equals(JsonSerializer.Deserialize<List<HoldingsServiceResponse>>(OkResponseContent));

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET {RequestUri} completed. Status Code: {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(7, HttpStatusCode.NotFound, ErrorNotFoundContent)]
        [TestCase(0, HttpStatusCode.BadRequest, ErrorBadRequestContent)]
        public async Task WhenHoldigsNotFoundOrInvalidForGivenLicenceId_ThenHoldingsServiceReturnsException(int licenceId, HttpStatusCode statusCode, string content)
        {
            var httpResponseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            A.CallTo(() => _fakeHoldingsServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);
            A.CallTo(() => _fakeHoldingsApiClient.GetHoldingsAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(httpResponseMessage);

            await FluentActions.Invoking(async () => await _holdingsService.GetHoldingsAsync(licenceId, CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to HoldingsService GET {RequestUri} failed. Status Code: {StatusCode} | Error Details: {Errors}.");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET {RequestUri} started."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public async Task WhenHoldingsServiceResponseOtherThanOkAndBadRequest_ThenReturnsException(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeHoldingsApiClient.GetHoldingsAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    RequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri(FakeUri)
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                });
            A.CallTo(() => _fakeHoldingsServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            await FluentActions.Invoking(async () => await _holdingsService.GetHoldingsAsync(23, CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to HoldingsService GET {RequestUri} failed. Status Code: {StatusCode}.");

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                  ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET {RequestUri} started."
              ).MustHaveHappenedOnceExactly();
        }
    }
}
