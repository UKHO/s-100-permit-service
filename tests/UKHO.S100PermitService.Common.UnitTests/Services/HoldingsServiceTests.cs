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
using UKHO.S100PermitService.Common.Handlers;
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
        private IOptions<WaitAndRetryConfiguration> _fakeWaitAndRetryConfiguration;
        private IHoldingsServiceAuthTokenProvider _fakeHoldingsServiceAuthTokenProvider;
        private IHoldingsApiClient _fakeHoldingsApiClient;
        private IHoldingsService _holdingsService;
        private IWaitAndRetryPolicy _fakeWaitAndRetryPolicy;
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
            _fakeWaitAndRetryConfiguration = Options.Create(new WaitAndRetryConfiguration() { RetryCount = "2", SleepDurationInSeconds = "2" });

            _fakeWaitAndRetryPolicy = new WaitAndRetryPolicy(_fakeWaitAndRetryConfiguration);
            _holdingsService = new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullHoldingsLogger = () => new HoldingsService(null, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy);
            nullHoldingsLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullHoldingsApiConfiguration = () => new HoldingsService(_fakeLogger, null, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy);
            nullHoldingsApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsApiConfiguration");

            Action nullAuthHoldingsServiceTokenProvider = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, null, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy);
            nullAuthHoldingsServiceTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsServiceAuthTokenProvider");

            Action nullHoldingsApiClient = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, null, _fakeWaitAndRetryPolicy);
            nullHoldingsApiClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsApiClient");

            Action nullWaitAndRetryPolicy = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, null);
            nullWaitAndRetryPolicy.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("waitAndRetryPolicy");
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

        [Test]
        [TestCase(HttpStatusCode.TooManyRequests, "TooManyRequests")]
        public void WhenHoldingsServiceResponseTooManyRequests_ThenReturnsException(HttpStatusCode statusCode, string content)
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

            Assert.ThrowsAsync<PermitServiceException>(() => _holdingsService.GetHoldingsAsync(23, CancellationToken.None, _fakeCorrelationId));

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
              && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientHoldingsRequest.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                  ["{OriginalFormat}"].ToString() == "Re-trying service request for Uri: {RequestUri} with delay: {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
              ).MustHaveHappened();
        }

        [Test]
        [TestCase("MultipleUpdateNumber")]
        [TestCase("DifferentExpiry")]
        [TestCase("DuplicateDataset")]
        public void WhenHoldingServiceResponseContainsDuplicateDatasetsOrMultipleExpiry_ThenReturnsFilteredHoldingsByLatestExpiry(string holdingResponseType)
        {
            var holdingsServiceResponse = GetHoldingsServiceResponse(holdingResponseType);

            var result = _holdingsService.FilterHoldingsByLatestExpiry(holdingsServiceResponse);

            result.Should().HaveCount(2);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.HoldingsTotalCellCount.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Holdings total cell count : {Count}"
            ).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.HoldingsFilteredCellCount.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Holdings filtered cell count : {Count}"
            ).MustHaveHappened();
        }

        private static List<HoldingsServiceResponse> GetHoldingsServiceResponse(string holdingResponseType)
        {
            return holdingResponseType switch
            {
                //Duplicate dataset with different edition number
                "MultipleUpdateNumber" => [
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle",
                            ProductCode = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle",
                                    CellCode = "CellCode",
                                    LatestEditionNumber = "2",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle1",
                            ProductCode = "ProductCode1",
                            ExpiryDate = DateTime.UtcNow.AddDays(4),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle1",
                                    CellCode = "CellCode1",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle",
                            ProductCode = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(3),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle",
                                    CellCode = "CellCode",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        }
                    ],

                //Duplicate dataset with different expiry
                "DifferentExpiry" => [
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle",
                            ProductCode = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle",
                                    CellCode = "CellCode",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle",
                            ProductCode = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(3),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle",
                                    CellCode = "CellCode",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle1",
                            ProductCode = "ProductCode1",
                            ExpiryDate = DateTime.UtcNow.AddDays(4),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle1",
                                    CellCode = "CellCode1",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        }
                    ],

                //Duplicate dataset
                "DuplicateDataset" => [
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle",
                            ProductCode = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle",
                                    CellCode = "CellCode",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle",
                            ProductCode = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle",
                                    CellCode = "CellCode",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            ProductTitle = "ProductTitle1",
                            ProductCode = "ProductCode1",
                            ExpiryDate = DateTime.UtcNow.AddDays(4),
                            Cells =
                            [
                                new Cell
                                {
                                    CellTitle = "CellTitle1",
                                    CellCode = "CellCode1",
                                    LatestEditionNumber = "1",
                                    LatestUpdateNumber = "1"
                                }
                            ]
                        }
                    ],

                _ => []
            };
        }
    }
}