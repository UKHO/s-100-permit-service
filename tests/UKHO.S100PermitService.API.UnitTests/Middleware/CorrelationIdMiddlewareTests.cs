using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common;
using Constants = UKHO.S100PermitService.Common.Constants;

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
        public async Task WhenCorrelationIdKeyDoesNotExistInRequestBody_ThenGenerateNewCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString();
            await _middleware.InvokeAsync(_fakeHttpContext);
            A.CallTo(() => _fakeLogger.BeginScope(A<Dictionary<string, object>>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenInvokeAsyncIsCalled_ThenNextMiddlewareShouldBeInvoked()
        {
            await _middleware.InvokeAsync(_fakeHttpContext);
            A.CallTo(() => _fakeNextMiddleware(_fakeHttpContext)).MustHaveHappenedOnceExactly();
        }
    }
}
