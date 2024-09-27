using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Clients;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.UnitTests.Handler;

namespace UKHO.S100PermitService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class UserPermitApiClientTests
    {
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        private const string ValidResponse = "[{\r\n  \"licenceId\": \"1\",\r\n  \"userPermits\": [\r\n    {\r\n      \"title\": \"Port Radar\",\r\n      \"upn\": \"FE5A853DEF9E83C9FFEF5AA001478103DB74C038A1B2C3\"\r\n    }\r\n  ]\r\n}]";
        private IHttpClientFactory _fakeHttpClientFactory;

        private IUserPermitApiClient? _userPermitApiClient;

        [SetUp]
        public void SetUp()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        [Test]
        public void WhenValidDataIsPassed_ThenUserPermitServiceReturnsOKResponse()
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                ValidResponse, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _userPermitApiClient = new UserPermitApiClient(_fakeHttpClientFactory);

            var result = _userPermitApiClient.GetUserPermitsAsync("http://test.com", 1, "testToken", CancellationToken.None, _fakeCorrelationId);

            var deSerializedResult = JsonConvert.DeserializeObject<List<UserPermitServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deSerializedResult!.Count.Should().BeGreaterThanOrEqualTo(1);
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.ServiceUnavailable)]
        [TestCase(HttpStatusCode.UnsupportedMediaType)]
        public void WhenUserPermitServiceResponseOtherThanOk(HttpStatusCode httpStatusCode)
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonConvert.SerializeObject(new UserPermitServiceResponse() { }), httpStatusCode);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _userPermitApiClient = new UserPermitApiClient(_fakeHttpClientFactory);

            var result = _userPermitApiClient.GetUserPermitsAsync("http://test.com", 0, string.Empty, CancellationToken.None, _fakeCorrelationId);

            result.Result.StatusCode.Should().Be(httpStatusCode);
        }
    }
}
