using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using System.Net;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models.ProductkeyService;
using UKHO.S100PermitService.Common.UnitTests.Handler;

namespace UKHO.S100PermitService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class ProductkeyServiceApiClientTests
    {
        private IHttpClientFactory _fakeHttpClientFactory;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();

        private IProductkeyServiceApiClient? _productkeyServiceApiClient;

        [SetUp]
        public void SetUp()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        [Test]
        public void WhenValidDataIsPassed_ThenProductKeyServiceReturnsOKResponse()
        {
            var productKeyServiceRequestData = JsonConvert.SerializeObject(new List<ProductKeyServiceRequest>() { new() { ProductName = "test101", Edition = "1" } });

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                                    JsonConvert.SerializeObject(new List<ProductKeyServiceResponse>()
                                    { new() { ProductName = "test101", Edition = "1", Key = "123456"} }), HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _productkeyServiceApiClient = new ProductkeyServiceApiClient(_fakeHttpClientFactory);

            var result = _productkeyServiceApiClient.CallProductkeyServiceApiAsync("http://test.com", HttpMethod.Post, productKeyServiceRequestData, "testToken", _fakeCorrelationId);

            var deSerializedResult = JsonConvert.DeserializeObject<List<ProductKeyServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deSerializedResult!.Count.Should().BeGreaterThanOrEqualTo(1);
            deSerializedResult![0].Key.Should().Be("123456");
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.ServiceUnavailable)]
        [TestCase(HttpStatusCode.UnsupportedMediaType)]
        public void WhenProductKeyServiceResponseOtherThanOk(HttpStatusCode httpStatusCode)
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonConvert.SerializeObject(new List<ProductKeyServiceResponse>() { new() }), httpStatusCode);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _productkeyServiceApiClient = new ProductkeyServiceApiClient(_fakeHttpClientFactory);

            var result = _productkeyServiceApiClient.CallProductkeyServiceApiAsync("http://test.com", HttpMethod.Post, "", string.Empty, _fakeCorrelationId);

            result.Result.StatusCode.Should().Be(httpStatusCode);
        }
    }
}