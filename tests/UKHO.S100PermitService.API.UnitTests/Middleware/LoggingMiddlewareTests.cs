using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common.Enum;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Exception;

namespace UKHO.S100PermitService.API.UnitTests.Middleware
{
    public class LoggingMiddlewareTests
    {
        private RequestDelegate _fakeNextMiddleware;
        private LoggingMiddleware _middleware;
        private HttpContext _fakeHttpContext;
        private ILogger<LoggingMiddleware> _fakeLogger;

        [SetUp]
        public void SetUp()
        {
            _fakeNextMiddleware = A.Fake<RequestDelegate>();
            _fakeLogger = A.Fake<ILogger<LoggingMiddleware>>();
            _middleware = new LoggingMiddleware(_fakeNextMiddleware, _fakeLogger);
            _fakeHttpContext = A.Fake<HttpContext>();
        }     

        [Test]
        public async Task WhenInvokeAsyncIsCalled_ThenNextMiddlewareShouldBeInvoked()
        {
            var bodyAsJson = new JObject { { "data", new JObject { } } };
            var bodyAsText = bodyAsJson.ToString();

            _fakeHttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyAsText));
            _fakeHttpContext.Request.ContentLength = bodyAsText.Length;
            _fakeHttpContext.Response.Body = new MemoryStream();

            await _middleware.InvokeAsync(_fakeHttpContext);

            A.CallTo(() => _fakeNextMiddleware(_fakeHttpContext)).MustHaveHappenedOnceExactly();
        }
    }
}
