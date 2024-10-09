using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
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
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;
using UKHO.S100PermitService.Common.Validations;

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

        private const string FakeUri = "http://test.com";
        private const string AccessToken = "access-token";

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
            var userPermitServiceResponse = new UserPermitServiceResponse { LicenceId = 1, UserPermits = [new UserPermit { Title = "Port Radar", Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3" }] };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(userPermitServiceResponse))
            };

            A.CallTo(() => _fakeUserPermitServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            A.CallTo(() => _fakeUserPermitApiClient.GetUserPermitsAsync
                (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(httpResponseMessage);

            var response = await _userPermitService.GetUserPermitAsync(1, CancellationToken.None, _fakeCorrelationId);

            response.Should().NotBeNull();
            response.Equals(userPermitServiceResponse);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} completed. StatusCode: {StatusCode}"
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

            await FluentActions.Invoking(async () => await _userPermitService.GetUserPermitAsync(licenceId, CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to UserPermitService GET {RequestUri} failed. StatusCode: {StatusCode} | Errors Details: {Errors}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
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

            await FluentActions.Invoking(async () => await _userPermitService.GetUserPermitAsync(4, CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to UserPermitService GET {RequestUri} failed. Status Code: {StatusCode}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.TooManyRequests, "TooManyRequests")]
        public async Task WhenUserPermitServiceResponseTooManyRequests_ThenResponseShouldNotBeOkAsync(HttpStatusCode statusCode, string content)
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
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientUserPermitRequest.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)
                  ["{OriginalFormat}"].ToString() == "Re-trying service request for Uri: {RequestUri} with delay: {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
              ).MustHaveHappened();
        }

        [Test]
        public void WhenUpnOrChecksumValidationFails_ThenThrowPermitServiceException()
        {
            A.CallTo(() => _fakeUserPermitValidator.Validate(A<UserPermitServiceResponse>.Ignored))
                .Returns(new ValidationResult(new[]
                {
                    new ValidationFailure("ErrorMessage", "Invalid checksum")
                }));

            FluentActions.Invoking(() => _userPermitService.ValidateUpnsAndChecksum(GeUserPermitServiceResponse())).Should().Throw<PermitServiceException>().WithMessage("Error(s) found for Licence Id: 1, Invalid checksum");
        }

        [Test]
        public void GetUpnInfoList_ShouldReturnCorrectList()
        {
            var expectedUpnInfoList = new List<UpnInfo>
            {
                new()
                {
                    EncryptedHardwareId = "FE5A853DEF9E83C9FFEF5AA001478103",
                    Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3",
                    MId = "A1B2C3",
                    Crc32 = "DB74C038"
                },
                new()
                {
                    EncryptedHardwareId = "869D4E0E902FA2E1B934A3685E5D0E85",
                    Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6",
                    MId = "D4E5F6",
                    Crc32 = "C1FDEC8B"
                },
                new()
                {
                    EncryptedHardwareId = "7B5CED73389DECDB110E6E803F957253",
                    Upn = "7B5CED73389DECDB110E6E803F957253F0DE13D1G7H8I9",
                    MId = "G7H8I9",
                    Crc32 = "F0DE13D1"
                }
            };

            var response = _userPermitService.MapUserPermitResponse(GeUserPermitServiceResponse());

            response.Equals(true);

            response.Count.Equals(expectedUpnInfoList.Count);

            for(var i = 0 ; i < expectedUpnInfoList.Count ; i++)
            {
                response[i].EncryptedHardwareId.Equals(expectedUpnInfoList[i].EncryptedHardwareId);
                response[i].Crc32.Equals(expectedUpnInfoList[i].Crc32);
                response[i].MId.Equals(expectedUpnInfoList[i].MId);
                response[i].Upn.Equals(expectedUpnInfoList[i].Upn);
                response[i].Title.Equals(expectedUpnInfoList[i].Title);
            }
        }

        private static UserPermitServiceResponse GeUserPermitServiceResponse()
        {
            return new UserPermitServiceResponse()
            {
                LicenceId = 1,
                UserPermits = [ new UserPermit{ Title = "Aqua Radar", Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3" },
                    new UserPermit{  Title= "SeaRadar X", Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6" },
                    new UserPermit{ Title = "Navi Radar", Upn = "7B5CED73389DECDB110E6E803F957253F0DE13D1G7H8I9" }
                ]
            };
        }
    }
}