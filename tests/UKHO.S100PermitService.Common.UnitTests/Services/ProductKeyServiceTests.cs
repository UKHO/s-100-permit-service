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
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Providers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.UnitTests.Services
{
    [TestFixture]
    public class ProductKeyServiceTests
    {
        private ILogger<ProductKeyService> _fakeLogger;
        private IOptions<ProductKeyServiceApiConfiguration> _fakeProductKeyServiceApiConfiguration;
        private IProductKeyServiceAuthTokenProvider _fakeProductKeyServiceAuthTokenProvider;
        private IProductKeyServiceApiClient _fakeProductKeyServiceApiClient;
        private readonly string _fakeCorrelationId = Guid.NewGuid().ToString();
        private const string RequestError = "{\"correlationId\":\"fc771eb4-926b-4965-8de9-8b37288d3bd0\",\"errors\":[{\"source\":\"GetProductKey\",\"description\":\"Key not found for ProductName: test101 and Edition: 1.\"}]}";
        private const string AccessToken = "access-token";

        private IProductKeyService _productKeyService;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<ProductKeyService>>();
            _fakeProductKeyServiceApiConfiguration = Options.Create(new ProductKeyServiceApiConfiguration() { ClientId = "ClientId2", BaseUrl = "http://localhost:5000" });
            _fakeProductKeyServiceAuthTokenProvider = A.Fake<IProductKeyServiceAuthTokenProvider>();
            _fakeProductKeyServiceApiClient = A.Fake<IProductKeyServiceApiClient>();

            _productKeyService = new ProductKeyService(_fakeLogger, _fakeProductKeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, _fakeProductKeyServiceApiClient);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullProductKeyServiceLogger = () => new ProductKeyService(null, _fakeProductKeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, _fakeProductKeyServiceApiClient);
            nullProductKeyServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullProductKeyServiceApiConfiguration = () => new ProductKeyService(_fakeLogger, null, _fakeProductKeyServiceAuthTokenProvider, _fakeProductKeyServiceApiClient);
            nullProductKeyServiceApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyServiceApiConfiguration");

            Action nullProductKeyServiceAuthTokenProvider = () => new ProductKeyService(_fakeLogger, _fakeProductKeyServiceApiConfiguration, null, _fakeProductKeyServiceApiClient);
            nullProductKeyServiceAuthTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyServiceAuthTokenProvider");

            Action nullProductKeyServiceApiClient = () => new ProductKeyService(_fakeLogger, _fakeProductKeyServiceApiConfiguration, _fakeProductKeyServiceAuthTokenProvider, null);
            nullProductKeyServiceApiClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("productKeyServiceApiClient");
        }

        [Test]
        public async Task WhenRequestsValidData_ThenProductKeyServiceReturnsValidResponse()
        {
            A.CallTo(() => _fakeProductKeyServiceApiClient.GetProductKeysAsync
                    (A<string>.Ignored, A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                            .Returns(new HttpResponseMessage()
                            {
                                StatusCode = HttpStatusCode.OK,
                                RequestMessage = new HttpRequestMessage()
                                {
                                    RequestUri = new Uri("http://test.com")
                                },
                                Content = new StringContent(JsonConvert.SerializeObject(new List<ProductKeyServiceResponse>() { new() { ProductName = "test101", Edition = "1", Key = "123456" } }))
                            });

            A.CallTo(() => _fakeProductKeyServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            var response = await _productKeyService.GetPermitKeysAsync([new() { ProductName = "test101", Edition = "1" }], CancellationToken.None, _fakeCorrelationId);
            response.Count.Should().BeGreaterThanOrEqualTo(1);
            response.Equals(new List<ProductKeyServiceResponse>() { new() { ProductName = "test101", Edition = "1", Key = "123456" } });

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProductKeyServicePostPermitKeyRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProductKeyServicePostPermitKeyRequestCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} completed. | StatusCode : {StatusCode}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        public async Task WhenRequestIsInvalidOrNonExistData_ThenThrowException(HttpStatusCode httpStatusCode)
        {
            A.CallTo(() => _fakeProductKeyServiceApiClient.GetProductKeysAsync
                    (A<string>.Ignored, A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                            .Returns(new HttpResponseMessage()
                            {
                                StatusCode = httpStatusCode,
                                RequestMessage = new HttpRequestMessage()
                                {
                                    RequestUri = new Uri("http://test.com")
                                },
                                Content = new StringContent(RequestError)
                            });

            A.CallTo(() => _fakeProductKeyServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            await FluentActions.Invoking(async () => await _productKeyService.GetPermitKeysAsync([], CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to ProductKeyService POST Uri : {0} failed. | StatusCode : {1} | Error Details : {2}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProductKeyServicePostPermitKeyRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        [TestCase(HttpStatusCode.UnsupportedMediaType, "UnsupportedMediaType")]
        public async Task WhenProductKeyServiceResponseOtherThanOk_ThenThrowException(HttpStatusCode httpStatusCode, string content)
        {
            A.CallTo(() => _fakeProductKeyServiceApiClient.GetProductKeysAsync
                    (A<string>.Ignored, A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                                .Returns(new HttpResponseMessage()
                                {
                                    StatusCode = httpStatusCode,
                                    RequestMessage = new HttpRequestMessage()
                                    {
                                        RequestUri = new Uri("http://test.com")
                                    },
                                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                                });

            A.CallTo(() => _fakeProductKeyServiceAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .Returns(AccessToken);

            await FluentActions.Invoking(async () => await _productKeyService.GetPermitKeysAsync([], CancellationToken.None, _fakeCorrelationId)).Should().ThrowAsync<PermitServiceException>().WithMessage("Request to ProductKeyService POST Uri : {0} failed. | StatusCode : {1}");

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.ProductKeyServicePostPermitKeyRequestStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to ProductKeyService POST Uri : {RequestUri} started."
            ).MustHaveHappenedOnceExactly();
        }
    }
}