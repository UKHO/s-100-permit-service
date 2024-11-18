using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Factories;
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
        private IUriFactory _fakeUriFactory;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        const string FakeUri = "http://test.com";
        const string AccessToken = "access-token";

        private const string OkResponseContent = "[\r\n  {\r\n    \"productCode\": \"P1231\",\r\n    \"productTitle\": \"P1231\",\r\n    \"expiryDate\": \"2026-01-31T23:59:00Z\",\r\n    \"cells\": [\r\n      {\r\n        \"cellCode\": \"1\",\r\n        \"cellTitle\": \"1\",\r\n        \"latestEditionNumber\": \"1\",\r\n        \"latestUpdateNumber\": \"11\"\r\n      }\r\n    ]\r\n  }\r\n]";
        private const string ErrorBadRequestContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetHoldings\",\r\n      \"description\": \"Incorrect LicenceId\"\r\n    }\r\n  ]\r\n}";
        private const string ErrorNotFoundContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetHoldings\",\r\n      \"description\": \"Licence Not Found\"\r\n    }\r\n  ]\r\n}";
        private const string NoContentResponse = "{[\r\n  {\r\n    \"unitName\": \"P1231\",\r\n    \"unitTitle\": \"P1231\",\r\n    \"expiryDate\": \"2026-01-31T23:59:00Z\",\r\n    \"datasets\": []\r\n  },\r\n  {\r\n    \"unitName\": \"P1232\",\r\n    \"unitTitle\": \"P1232\",\r\n    \"expiryDate\": \"2026-01-31T23:59:00Z\",\r\n    \"datasets\": []\r\n  }\r\n]}";

        [SetUp]
        public void Setup()
        {
            _fakeHoldingsServiceApiConfiguration = Options.Create(new HoldingsServiceApiConfiguration { ClientId = "ClientId2", BaseUrl = FakeUri, RequestTimeoutInMinutes = 5 });
            _fakeHoldingsApiClient = A.Fake<IHoldingsApiClient>();
            _fakeHoldingsServiceAuthTokenProvider = A.Fake<IHoldingsServiceAuthTokenProvider>();
            _fakeLogger = A.Fake<ILogger<HoldingsService>>();
            _fakeWaitAndRetryConfiguration = Options.Create(new WaitAndRetryConfiguration() { RetryCount = "2", SleepDurationInSeconds = "2" });

            _fakeWaitAndRetryPolicy = new WaitAndRetryPolicy(_fakeWaitAndRetryConfiguration);
            _fakeUriFactory = A.Fake<UriFactory>();
            _holdingsService = new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy, _fakeUriFactory);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullHoldingsLogger = () => new HoldingsService(null, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy, _fakeUriFactory);
            nullHoldingsLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullHoldingsApiConfiguration = () => new HoldingsService(_fakeLogger, null, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy, _fakeUriFactory);
            nullHoldingsApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsApiConfiguration");

            Action nullAuthHoldingsServiceTokenProvider = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, null, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy, _fakeUriFactory);
            nullAuthHoldingsServiceTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsServiceAuthTokenProvider");

            Action nullHoldingsApiClient = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, null, _fakeWaitAndRetryPolicy, _fakeUriFactory);
            nullHoldingsApiClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("holdingsApiClient");

            Action nullWaitAndRetryPolicy = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, null, _fakeUriFactory);
            nullWaitAndRetryPolicy.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("waitAndRetryPolicy");

            Action nullUriFactory = () => new HoldingsService(_fakeLogger, _fakeHoldingsServiceApiConfiguration, _fakeHoldingsServiceAuthTokenProvider, _fakeHoldingsApiClient, _fakeWaitAndRetryPolicy, null);
            nullUriFactory.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("uriFactory");
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

            var (httpResponseMessage, holdingsServiceResponse) = await _holdingsService.GetHoldingsAsync(1, CancellationToken.None, _fakeCorrelationId);
            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            holdingsServiceResponse.Count().Should().BeGreaterThanOrEqualTo(1);
            holdingsServiceResponse.Should().BeEquivalentTo(JsonSerializer.Deserialize<List<HoldingsServiceResponse>>(OkResponseContent));

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestCompletedWithOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} completed. | StatusCode: {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenLicenceIsValidButHoldingsAreNotAvailable_ThenHoldingsServiceReturns204NoContentResponse()
        {
            A.CallTo(() => _fakeHoldingsApiClient.GetHoldingsAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NoContent,
                    RequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri(FakeUri)
                    },
                    Content = new StringContent(NoContentResponse)
                });
            A.CallTo(() => _fakeHoldingsServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            var (getHoldingsHttpResponseMessage, holdingsServiceResponse) = await _holdingsService.GetHoldingsAsync(1, CancellationToken.None, _fakeCorrelationId);

            getHoldingsHttpResponseMessage.StatusCode.Should().Be(HttpStatusCode.NoContent);
            holdingsServiceResponse.Should().BeNull();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Warning
            && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestCompletedWithNoContent.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} responded with empty response completed. | StatusCode: {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenLicenceIdNotFound_ThenHoldingsServiceReturns404NotFoundResponse()
        {
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(ErrorNotFoundContent, Encoding.UTF8, "application/json")
            };

            A.CallTo(() => _fakeHoldingsServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                    .Returns(AccessToken);

            A.CallTo(() => _fakeHoldingsApiClient.GetHoldingsAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                    .Returns(httpResponseMessage);

            var (getHoldingsHttpResponseMessage, holdingsServiceResponse) = await _holdingsService.GetHoldingsAsync(14, CancellationToken.None, _fakeCorrelationId);

            getHoldingsHttpResponseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
            holdingsServiceResponse.Should().BeNull();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                    && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                    && call.GetArgument<EventId>(1) == EventIds.HoldingServiceGetHoldingsLicenceNotFound.ToEventId()
                    && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode} | Warning Response: {Response}"
                ).MustHaveHappenedOnceExactly();
        }

        [TestCase(0, HttpStatusCode.BadRequest, ErrorBadRequestContent)]
        [TestCase(-2, HttpStatusCode.BadRequest, ErrorBadRequestContent)]
        public async Task WhenLicenceIdIs0OrNegative_ThenHoldingsServiceReturnsBadRequestResponse(int licenceId, HttpStatusCode statusCode, string content)
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

            var (getHoldingsHttpResponseMessage, holdingsServiceResponse) = await _holdingsService.GetHoldingsAsync(licenceId, CancellationToken.None, _fakeCorrelationId);

            getHoldingsHttpResponseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            holdingsServiceResponse.Should().BeNull();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestReturnsBadRequest.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode} | Warning Response: {Response}"
            ).MustHaveHappenedOnceExactly();
        }

        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public async Task WhenHoldingsServiceResponseOtherThanOkBadRequestAndNotFound_ThenReturnsException(HttpStatusCode statusCode, string content)
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

            await FluentActions.Invoking(async () => await _holdingsService.GetHoldingsAsync(23, CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to HoldingsService GET Uri : {RequestUri} failed. | StatusCode: {StatusCode}");

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.HoldingsServiceGetHoldingsRequestStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                  ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} started."
              ).MustHaveHappenedOnceExactly();
        }

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
                  ["{OriginalFormat}"].ToString() == "Request to HoldingsService GET Uri : {RequestUri} started."
              ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientHoldingsRequest.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                  ["{OriginalFormat}"].ToString() == "Re-trying service request for Uri: {RequestUri} with delay: {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
              ).MustHaveHappened();
        }

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
                && call.GetArgument<EventId>(1) == EventIds.HoldingsFilteredCellCount.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() == "Filtered holdings: Total count before filtering: {TotalCellCount}, after filtering for highest expiry dates and removing duplicates: {FilteredCellCount}."
            ).MustHaveHappened();
        }

        [ExcludeFromCodeCoverage]
        private static List<HoldingsServiceResponse> GetHoldingsServiceResponse(string holdingResponseType)
        {
            return holdingResponseType switch
            {
                //Duplicate dataset with different edition number
                "MultipleUpdateNumber" => [
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle",
                            UnitName = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 2,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle1",
                            UnitName = "ProductCode1",
                            ExpiryDate = DateTime.UtcNow.AddDays(4),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle1",
                                    DatasetName = "CellCode1",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle",
                            UnitName = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(3),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        }
                    ],

                //Duplicate dataset with different expiry
                "DifferentExpiry" => [
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle",
                            UnitName = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle",
                            UnitName = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(3),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle1",
                            UnitName = "ProductCode1",
                            ExpiryDate = DateTime.UtcNow.AddDays(4),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle1",
                                    DatasetName = "CellCode1",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        }
                    ],

                //Duplicate dataset
                "DuplicateDataset" => [
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle",
                            UnitName = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle",
                            UnitName = "ProductCode",
                            ExpiryDate = DateTime.UtcNow.AddDays(5),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle",
                                    DatasetName = "CellCode",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        },
                        new HoldingsServiceResponse {
                            UnitTitle = "ProductTitle1",
                            UnitName = "ProductCode1",
                            ExpiryDate = DateTime.UtcNow.AddDays(4),
                            Datasets =
                            [
                                new Dataset
                                {
                                    DatasetTitle = "CellTitle1",
                                    DatasetName = "CellCode1",
                                    LatestEditionNumber = 1,
                                    LatestUpdateNumber = 1
                                }
                            ]
                        }
                    ],

                _ => []
            };
        }
    }
}