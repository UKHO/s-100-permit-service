using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class UserPermitServiceTests
    {
        private ILogger<UserPermitService> _fakeLogger;
        private IOptions<UserPermitServiceApiConfiguration> _fakeUserPermitServiceApiConfiguration;
        private IAuthUserPermitServiceTokenProvider _fakeAuthUserPermitServiceTokenProvider;
        private IUserPermitApiClient _fakeUserPermitApiClient;
        private UserPermitService _userPermitService;

        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private const string ErrorNotFoundContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetUserPermits\",\r\n      \"description\": \"User permits not found for given LicenceId\"\r\n    }\r\n  ]\r\n}";
        private const string ErrorBadRequestContent = "{\r\n  \"errors\": [\r\n    {\r\n      \"source\": \"GetUserPermits\",\r\n      \"description\": \"LicenceId is incorrect\"\r\n    }\r\n  ]\r\n}";

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<UserPermitService>>();
            _fakeUserPermitServiceApiConfiguration = Options.Create(new UserPermitServiceApiConfiguration() { ClientId = "ClientId", BaseUrl = "http://localhost:5000" });
            _fakeAuthUserPermitServiceTokenProvider = A.Fake<IAuthUserPermitServiceTokenProvider>();
            _fakeUserPermitApiClient = A.Fake<IUserPermitApiClient>();

            _userPermitService = new UserPermitService(_fakeLogger, _fakeUserPermitServiceApiConfiguration, _fakeAuthUserPermitServiceTokenProvider, _fakeUserPermitApiClient);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullUserPermitServiceLogger = () => new UserPermitService(null, _fakeUserPermitServiceApiConfiguration, _fakeAuthUserPermitServiceTokenProvider, _fakeUserPermitApiClient);
            nullUserPermitServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullUserPermitServiceApiConfiguration = () => new UserPermitService(_fakeLogger, null, _fakeAuthUserPermitServiceTokenProvider, _fakeUserPermitApiClient);
            nullUserPermitServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitServiceApiConfiguration");

            Action nullAuthUserPermitServiceTokenProvider = () => new UserPermitService(_fakeLogger, _fakeUserPermitServiceApiConfiguration, null, _fakeUserPermitApiClient);
            nullAuthUserPermitServiceTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("authUserPermitServiceTokenProvider");

            Action nullUserPermitApiClient = () => new UserPermitService(_fakeLogger, _fakeUserPermitServiceApiConfiguration, _fakeAuthUserPermitServiceTokenProvider, null);
            nullUserPermitApiClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userPermitApiClient");
        }

        [Test]
        public async Task WhenValidLicenceId_ThenUserPermitServiceReturns200OKResponse()
        {
            const int LicenceId = 1;
            const string AccessToken = "access-token";

            var userPermitServiceResponse = new UserPermitServiceResponse { LicenceId = "1", UserPermits = [new UserPermit { Title = "Port Radar", Upn = "FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3" }] };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(userPermitServiceResponse))
            };

            A.CallTo(() => _fakeAuthUserPermitServiceTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            A.CallTo(() => _fakeUserPermitApiClient.GetUserPermitsAsync
                (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponseMessage);

            var response = await _userPermitService.GetUserPermitAsync(LicenceId, _fakeCorrelationId);

            Assert.IsNotNull(response);
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
        public Task WhenLicenceIdNotFoundOr0_ThenUserPermitServiceReturnsException404Or400WithErrorDetails(int licenceId, HttpStatusCode statusCode, string content)
        {
            const string AccessToken = "access-token";

            var httpResponseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            A.CallTo(() => _fakeAuthUserPermitServiceTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            A.CallTo(() => _fakeUserPermitApiClient.GetUserPermitsAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponseMessage);

            Assert.ThrowsAsync<Exception>(() => _userPermitService.GetUserPermitAsync(licenceId, _fakeCorrelationId));

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} failed. StatusCode: {StatusCode} | Errors Details: {ErrorDetails}"
            ).MustHaveHappenedOnceExactly();

            return Task.CompletedTask;
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public Task WhenUserPermitServiceResponseOtherThanOk_ThenThrowExceptionWithoutErrorDetails(HttpStatusCode statusCode, string content)
        {
            const int LicenceId = 4;
            const string AccessToken = "access-token";

            var httpResponseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            A.CallTo(() => _fakeAuthUserPermitServiceTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            A.CallTo(() => _fakeUserPermitApiClient.GetUserPermitsAsync
                    (A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponseMessage);

            Assert.ThrowsAsync<Exception>(() => _userPermitService.GetUserPermitAsync(LicenceId, _fakeCorrelationId));

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} started"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.UserPermitServiceGetUserPermitsRequestFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to UserPermitService GET {RequestUri} failed. StatusCode: {StatusCode}"
            ).MustHaveHappenedOnceExactly();

            return Task.CompletedTask;
        }
    }
}