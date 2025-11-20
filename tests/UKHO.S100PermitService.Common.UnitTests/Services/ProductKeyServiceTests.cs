using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Factories;
using UKHO.S100PermitService.Common.Handlers;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class ProductKeyServiceTests
    {
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        private const string AccessToken = "access-token";

        private ILogger<ProductKeyService> _fakeLogger;
        private IOptions<ProductKeyServiceApiConfiguration> _fakeProductKeyServiceApiConfiguration;
        private IOptions<WaitAndRetryConfiguration> _fakeWaitAndRetryConfiguration;
        private IProductKeyServiceAuthTokenProvider _fakeProductKeyServiceAuthTokenProvider;
        private IProductKeyServiceApiClient _fakeProductKeyServiceApiClient;
        private IWaitAndRetryPolicy _fakeWaitAndRetryPolicy;
        private IUriFactory _fakeUriFactory;

        private IProductKeyService _productKeyService;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<ProductKeyService>>();
            _fakeProductKeyServiceApiConfiguration = Options.Create(new ProductKeyServiceApiConfiguration() { ClientId = "ClientId2", BaseUrl = "http://localhost:5000" });
            _fakeProductKeyServiceAuthTokenProvider = A.Fake<IProductKeyServiceAuthTokenProvider>();
            _fakeProductKeyServiceApiClient = A.Fake<IProductKeyServiceApiClient>();
            _fakeWaitAndRetryConfiguration = Options.Create(new WaitAndRetryConfiguration() { RetryCount = "2", SleepDurationInSeconds = "2" });

            _fakeWaitAndRetryPolicy = new WaitAndRetryPolicy(_fakeWaitAndRetryConfiguration);
            _fakeUriFactory = A.Fake<UriFactory>();
            _productKeyService = new ProductKeyService(_fakeLogger, _fakeProductKeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, _fakeProductKeyServiceApiClient, _fakeWaitAndRetryPolicy, _fakeUriFactory);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullProductKeyServiceLogger = () => new ProductKeyService(null, _fakeProductKeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, _fakeProductKeyServiceApiClient, _fakeWaitAndRetryPolicy, _fakeUriFactory);
            Assert.That(nullProductKeyServiceLogger, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("logger"));

            Action nullProductKeyServiceApiConfiguration = () => new ProductKeyService(_fakeLogger, null, _fakeProductKeyServiceAuthTokenProvider, _fakeProductKeyServiceApiClient, _fakeWaitAndRetryPolicy, _fakeUriFactory);
            Assert.That(nullProductKeyServiceApiConfiguration, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("productKeyServiceApiConfiguration"));

            Action nullProductKeyServiceAuthTokenProvider = () => new ProductKeyService(_fakeLogger, _fakeProductKeyServiceApiConfiguration, null, _fakeProductKeyServiceApiClient, _fakeWaitAndRetryPolicy, _fakeUriFactory);
            Assert.That(nullProductKeyServiceAuthTokenProvider, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("productKeyServiceAuthTokenProvider"));

            Action nullProductKeyServiceApiClient = () => new ProductKeyService(_fakeLogger, _fakeProductKeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, null, _fakeWaitAndRetryPolicy, _fakeUriFactory);
            Assert.That(nullProductKeyServiceApiClient, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("productKeyServiceApiClient"));

            Action nullWaitAndRetryClient = () => new ProductKeyService(_fakeLogger, _fakeProductKeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, _fakeProductKeyServiceApiClient, null, _fakeUriFactory);
            Assert.That(nullWaitAndRetryClient, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("waitAndRetryPolicy"));

            Action nullUriFactory = () => new ProductKeyService(_fakeLogger, _fakeProductKeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, _fakeProductKeyServiceApiClient, _fakeWaitAndRetryPolicy, null);
            Assert.That(nullUriFactory, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("uriFactory"));
        }

        [Test]
        public async Task WhenRequestsValidData_ThenProductKeyServiceReturnsValidResponse()
        {
            A.CallTo(() => _fakeProductKeyServiceApiClient.GetProductKeysAsync
                    (A<string>.Ignored, A<IEnumerable<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                            .Returns(new HttpResponseMessage()
                            {
                                StatusCode = HttpStatusCode.OK,
                                RequestMessage = new HttpRequestMessage()
                                {
                                    RequestUri = new Uri("http://test.com")
                                },
                                Content = new StringContent(JsonSerializer.Serialize(new List<ProductKeyServiceResponse>() { new() { ProductName = "test101", Edition = "1", Key = "123456" } }))
                            });

            A.CallTo(() => _fakeProductKeyServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
               .Returns(AccessToken);

            var response = await _productKeyService.GetProductKeysAsync([new() { ProductName = "test101", Edition = "1" }], _fakeCorrelationId, CancellationToken.None);

            using(Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.Value, Has.Exactly(1)
                         .Matches<ProductKeyServiceResponse>(p =>
                             p.ProductName == "test101" &&
                             p.Edition == "1" &&
                             p.Key == "123456"));
                Assert.That(response.Origin, Is.Null);
            }

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestCompletedWithStatus200Ok.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} completed. | StatusCode : {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest, "BadRequest")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        [TestCase(HttpStatusCode.UnsupportedMediaType, "UnsupportedMediaType")]
        public async Task WhenProductKeyServiceResponseOtherThanOk_ThenServiceReturnsFailureResponse(HttpStatusCode httpStatusCode, string content)
        {
            A.CallTo(() => _fakeProductKeyServiceApiClient.GetProductKeysAsync
                    (A<string>.Ignored, A<IEnumerable<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                                .Returns(new HttpResponseMessage()
                                {
                                    StatusCode = httpStatusCode,
                                    RequestMessage = new HttpRequestMessage()
                                    {
                                        RequestUri = new Uri("http://test.com")
                                    },
                                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new ErrorResponse { CorrelationId = _fakeCorrelationId, Errors = [new() { Source = "GetProductKey", Description = content }] }))))
                                    {
                                        Headers =
                                            {
                                                ContentType = new MediaTypeHeaderValue("application/json")
                                            }
                                    }
                                });

            var response = await _productKeyService.GetProductKeysAsync([new() { ProductName = "InValidProduct", Edition = "1" }], _fakeCorrelationId, CancellationToken.None);

            using(Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(httpStatusCode));
                Assert.That(response.ErrorResponse.Errors, Has.Exactly(1).Matches<ErrorDetail>(error => error.Source == "GetProductKey" && error.Description == content));
                Assert.That(response.Origin, Is.EqualTo(PermitServiceConstants.ProductKeyService));
            }

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} failed. | StatusCode : {StatusCode} | Error Details : {Errors}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestCompletedWithStatus200Ok.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} completed. | StatusCode : {StatusCode}"
            ).MustNotHaveHappened();
        }

        [Test]
        [TestCase(HttpStatusCode.TooManyRequests, "TooManyRequests")]
        public async Task WhenProductKeyServiceResponseTooManyRequests_ThenServiceReturnsFailureResponseWithRetry(HttpStatusCode httpStatusCode, string content)
        {
            A.CallTo(() => _fakeProductKeyServiceApiClient.GetProductKeysAsync
                    (A<string>.Ignored, A<IEnumerable<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                                .Returns(new HttpResponseMessage()
                                {
                                    StatusCode = httpStatusCode,
                                    RequestMessage = new HttpRequestMessage()
                                    {
                                        RequestUri = new Uri("http://test.com")
                                    },
                                    Content = new StreamContent(
                                        new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                                            new ErrorResponse { CorrelationId = _fakeCorrelationId, Errors = [new() { Source = "GetProductKey", Description = content }] })
                                        )))
                                    {
                                        Headers =
                                            {
                                                ContentType = new MediaTypeHeaderValue("application/json")
                                            }
                                    }
                                });

            A.CallTo(() => _fakeProductKeyServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            var response = await _productKeyService.GetProductKeysAsync([new() { ProductName = "InValidProduct", Edition = "1" }], _fakeCorrelationId, CancellationToken.None);

            using(Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(httpStatusCode));
                Assert.That(response.ErrorResponse.Errors, Has.Exactly(1).Matches<ErrorDetail>(error => error.Source == "GetProductKey" && error.Description == content));
                Assert.That(response.Origin, Is.EqualTo(PermitServiceConstants.ProductKeyService));
            }

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientProductKeyServiceRequest.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                  ["{OriginalFormat}"].ToString() == "Re-trying service request for Uri: {RequestUri} with delay: {Delay}ms and retry attempt {Retry} with _X-Correlation-ID:{CorrelationId} as previous request was responded with {StatusCode}."
              ).MustHaveHappened();
        }

        [Test]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task WhenProductKeyServiceResponseUnauthorizedOrForbiddenWithNoContent_ThenServiceReturnsFailureResponse(HttpStatusCode httpStatusCode)
        {
            A.CallTo(() => _fakeProductKeyServiceApiClient.GetProductKeysAsync
                    (A<string>.Ignored, A<IEnumerable<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                                .Returns(new HttpResponseMessage()
                                {
                                    StatusCode = httpStatusCode,
                                    RequestMessage = new HttpRequestMessage()
                                    {
                                        RequestUri = new Uri("http://test.com")
                                    },
                                });

            var response = await _productKeyService.GetProductKeysAsync([new() { ProductName = "InValidProduct", Edition = "1" }], _fakeCorrelationId, CancellationToken.None);

            using(Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(httpStatusCode));
                Assert.That(response.ErrorResponse.Errors, Is.Null);
                Assert.That(response.Origin, Is.EqualTo(PermitServiceConstants.ProductKeyService));
            }

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} failed. | StatusCode : {StatusCode} | Error Details : {Errors}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GetProductKeysRequestCompletedWithStatus200Ok.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} completed. | StatusCode : {StatusCode}"
            ).MustNotHaveHappened();
        }
    }
}