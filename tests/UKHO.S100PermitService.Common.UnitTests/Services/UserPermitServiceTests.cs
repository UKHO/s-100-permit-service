using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Handlers;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Validation;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class UserPermitServiceTests
    {
        private ILogger<UserPermitService> _fakeLogger;
        private IOptions<UserPermitServiceApiConfiguration> _fakeUserPermitServiceApiConfiguration;
        private IOptions<WaitAndRetryConfiguration> _fakeWaitAndRetryConfiguration;
        private IUserPermitServiceAuthTokenProvider _fakeUserPermitServiceAuthTokenProvider;
        private IUserPermitApiClient _fakeUserPermitApiClient;
        private IWaitAndRetryPolicy _fakeWaitAndRetryPolicy;
        private IUserPermitValidator _fakeUserPermitValidator;
        private UserPermitService _userPermitService;

        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        const string FakeUri = "http://test.com";
        const string AccessToken = "access-token";

        private const string ErrorNotFoundContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetUserPermits\",\r\n      \"description\": \"User permits not found for given LicenceId\"\r\n    }\r\n  ]\r\n}";
        private const string ErrorBadRequestContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetUserPermits\",\r\n      \"description\": \"LicenceId is incorrect\"\r\n    }\r\n  ]\r\n}";

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<UserPermitService>>();
            _fakeUserPermitServiceApiConfiguration = Options.Create(new UserPermitServiceApiConfiguration() { ClientId = "ClientId", BaseUrl = "http://localhost:5000", RequestTimeoutInMinutes = 5 });
            _fakeUserPermitServiceAuthTokenProvider = A.Fake<IUserPermitServiceAuthTokenProvider>();
            _fakeUserPermitApiClient = A.Fake<IUserPermitApiClient>();
            _fakeWaitAndRetryConfiguration = Options.Create(new WaitAndRetryConfiguration() { RetryCount = "2", SleepDurationInSeconds = "2" });

            _fakeWaitAndRetryPolicy = new WaitAndRetryPolicy(_fakeWaitAndRetryConfiguration);
            _fakeUserPermitValidator = A.Fake<IUserPermitValidator>();
            _userPermitService = new UserPermitService(_fakeLogger, _fakeUserPermitServiceApiConfiguration, _fakeUserPermitServiceAuthTokenProvider, _fakeUserPermitApiClient, _fakeWaitAndRetryPolicy, _fakeUserPermitValidator);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullUserPermitServiceLogger = () => new UserPermitService(null, _fakeUserPermitServiceApiConfiguration, _fakeUserPermitServiceAuthTokenProvider, _fakeUserPermitApiClient, _fakeWaitAndRetryPolicy, _fakeUserPermitValidator);
            nullUserPermitServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullUserPermitServiceApiConfiguration = () => new UserPermitService(_fakeLogger, null, _fakeUserPermitServiceAuthTokenProvider, _fakeUserPermitApiClient, _fakeWaitAndRetryPolicy, _fakeUserPermitValidator);
            nullUserPermitServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitServiceApiConfiguration");

            Action nullAuthUserPermitServiceTokenProvider = () => new UserPermitService(_fakeLogger, _fakeUserPermitServiceApiConfiguration, null, _fakeUserPermitApiClient, _fakeWaitAndRetryPolicy, _fakeUserPermitValidator);
            nullAuthUserPermitServiceTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitServiceAuthTokenProvider");

            Action nullUserPermitApiClient = () => new UserPermitService(_fakeLogger, _fakeUserPermitServiceApiConfiguration, _fakeUserPermitServiceAuthTokenProvider, null, _fakeWaitAndRetryPolicy, _fakeUserPermitValidator);
            nullUserPermitApiClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitApiClient");

            Action nullWaitAndRetryClient = () => new UserPermitService(_fakeLogger, _fakeUserPermitServiceApiConfiguration, _fakeUserPermitServiceAuthTokenProvider, _fakeUserPermitApiClient, null, _fakeUserPermitValidator);
            nullWaitAndRetryClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("waitAndRetryPolicy");

            Action nullUserPermitServiceValidator = () => new UserPermitService(_fakeLogger, _fakeUserPermitServiceApiConfiguration, _fakeUserPermitServiceAuthTokenProvider, _fakeUserPermitApiClient, _fakeWaitAndRetryPolicy, null);
            nullUserPermitServiceValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitValidator");
        }

        [Test]
        public async Task WhenValidDataIsPassed_ThenUserPermitServiceReturnsOkResponse()
        {
            const int LicenceId = 1;
            const string AccessToken = "access-token";

            var userPermitServiceResponse = new UserPermitServiceResponse { LicenceId = 1, UserPermits = [new UserPermit { Title = "Port Radar", Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3" }] };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(userPermitServiceResponse))
            };

            A.CallTo(() => _fakeUserPermitServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            A.CallTo(() => _fakeUserPermitApiClient.GetUserPermitsAsync
                (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(httpResponseMessage);

            var response = await _userPermitService.GetUserPermitAsync(LicenceId, CancellationToken.None, _fakeCorrelationId);

            response.Should().NotBeNull();
            response.Equals(userPermitServiceResponse);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} completed. StatusCode: {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(7, HttpStatusCode.NotFound, ErrorNotFoundContent)]
        [TestCase(0, HttpStatusCode.BadRequest, ErrorBadRequestContent)]
        public async Task WhenLicenceIdNotFoundOr0_ThenUserPermitServiceReturnsException404Or400WithErrorDetails(int licenceId, HttpStatusCode statusCode, string content)
        {
            var httpResponseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            A.CallTo(() => _fakeUserPermitServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            A.CallTo(() => _fakeUserPermitApiClient.GetUserPermitsAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(httpResponseMessage);

            await FluentActions.Invoking(async () => await _userPermitService.GetUserPermitAsync(licenceId, CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to UserPermitService GET {0} failed. StatusCode: {1} | Errors Details: {2}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public async Task WhenUserPermitServiceResponseOtherThanOk_ThenResponseShouldNotBeOk(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeUserPermitServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            A.CallTo(() => _fakeUserPermitApiClient.GetUserPermitsAsync
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

            await FluentActions.Invoking(async () => await _userPermitService.GetUserPermitAsync(4, CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to UserPermitService GET {0} failed. Status Code: {1}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.TooManyRequests, "TooManyRequests")]
        public void WhenUserPermitServiceResponseTooManyRequests_ThenResponseShouldNotBeOk(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeUserPermitServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            A.CallTo(() => _fakeUserPermitApiClient.GetUserPermitsAsync
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

            FluentActions.Invoking(() =>
                _userPermitService.GetUserPermitAsync(4, CancellationToken.None, _fakeCorrelationId))
                .Should().ThrowAsync<PermitServiceException>().WithMessage("Request to UserPermitService GET {0} failed. Status Code: {1}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientUserPermitRequest.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                  ["{OriginalFormat}"].ToString() == "Re-trying service request for Uri: {RequestUri} with delay: {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
              ).MustHaveHappened();
        }

        [Test]
        public async Task WhenValidUpns_ThenReturnsTrue()
        {
            A.CallTo(() => _fakeUserPermitValidator.Validate(A<UserPermitServiceResponse>.Ignored)).Returns(new ValidationResult());

            var response = _userPermitService.ValidateUpnsAndChecksum(GeUserPermitServiceResponse());

            response.Equals(true);
        }

        [Test]
        public void WhenUpnOrChecksumValidationFails_ThenThrowPermitServiceException()
        {
            A.CallTo(() => _fakeUserPermitValidator.Validate(A<UserPermitServiceResponse>.Ignored))
                .Returns(new ValidationResult(new[]
                {
                    new ValidationFailure("ErrorMessage", "Invalid checksum")
                }));

            FluentActions.Invoking(() => _userPermitService.ValidateUpnsAndChecksum(GeUserPermitServiceResponse())).Should().Throw<PermitServiceException>().WithMessage("Invalid checksum");
        }

        private static UserPermitServiceResponse GeUserPermitServiceResponse()
        {
            return new UserPermitServiceResponse()
            {
                LicenceId = 1,
                UserPermits = [ new UserPermit{ Title = "Aqua Radar", Upn = "EF1C61C926BD9F18F44897CA1A5214BE06F92FFiJ0K1L2" },
                    new UserPermit{  Title= "SeaRadar X", Upn = "E9FAE304D230E4C729288349DA29776EE9B57E01M3N4O5" },
                    new UserPermit{ Title = "Navi Radar", Upn = "F1EB202BDC150506E21E3E44FD1829424462D958P6Q7R8" }
                ]
            };
        }
    }
}