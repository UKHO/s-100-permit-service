using System.Text;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common;

namespace UKHO.S100PermitService.API.UnitTests.Middleware
{
    public class CorrelationIdMiddlewareTests
    {
        private RequestDelegate _fakeNextMiddleware;
        private CorrelationIdMiddleware _middleware;
        private HttpContext _fakeHttpContext;
        private ILogger<CorrelationIdMiddleware> _fakeLogger;

        [SetUp]
        public void SetUp()
        {
            _fakeNextMiddleware = A.Fake<RequestDelegate>();
            _middleware = new CorrelationIdMiddleware(_fakeNextMiddleware);
            _fakeLogger = A.Fake<ILogger<CorrelationIdMiddleware>>();
            _fakeHttpContext = A.Fake<HttpContext>();
            _fakeHttpContext.RequestServices = new ServiceCollection().AddSingleton(_fakeLogger).BuildServiceProvider();
        }

        [Test]
        public async Task WhenCorrelationIdKeyDoesNotExistInHeader_ThenGenerateNewCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString();

            await _middleware.InvokeAsync(_fakeHttpContext);

            A.CallTo(() => _fakeHttpContext.Request.Headers.Append(Constants.XCorrelationIdHeaderKey, correlationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeLogger.BeginScope(A<Dictionary<string, object>>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenCorrelationIdKeyExistsInHeader_ThenNextMiddlewareShouldBeInvokedWithSameCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString();
            _fakeHttpContext.Request.Headers[Constants.XCorrelationIdHeaderKey] = correlationId;

            await _middleware.InvokeAsync(_fakeHttpContext);

            A.CallTo(() => _fakeHttpContext.Response.Headers.ContainsKey(Constants.XCorrelationIdHeaderKey)).Returns(true);
            A.CallTo(() => _fakeHttpContext.Response.Headers[Constants.XCorrelationIdHeaderKey]).Returns(correlationId);
            A.CallTo(() => _fakeNextMiddleware(_fakeHttpContext)).MustHaveHappenedOnceExactly();
        }
    }
}
