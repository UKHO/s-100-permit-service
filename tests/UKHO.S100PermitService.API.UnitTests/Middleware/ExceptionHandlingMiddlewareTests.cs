using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Text.Json;
using UKHO.S100PermitService.API.Middleware;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using Assert = NUnit.Framework.Assert;

namespace UKHO.S100PermitService.API.UnitTests.Middleware
{
    [TestFixture]
    public class ExceptionHandlingMiddlewareTests
    {
        private RequestDelegate _fakeNextMiddleware;
        private HttpContext _fakeHttpContext;
        private ILogger<ExceptionHandlingMiddleware> _fakeLogger;
        private ExceptionHandlingMiddleware _middleware;

        [SetUp]
        public void SetUp()
        {
            _fakeNextMiddleware = A.Fake<RequestDelegate>();
            _fakeLogger = A.Fake<ILogger<ExceptionHandlingMiddleware>>();
            _fakeHttpContext = new DefaultHttpContext();

            _middleware = new ExceptionHandlingMiddleware(_fakeNextMiddleware, _fakeLogger);
        }

        [Test]
        public async Task WhenExceptionIsOfTypeException_ThenLogsErrorWithUnhandledExceptionEventId()
        {
            var memoryStream = new MemoryStream();
            _fakeHttpContext.Request.Headers.Append(PermitServiceConstants.XCorrelationIdHeaderKey, "fakeCorrelationId");
            _fakeHttpContext.Response.Body = memoryStream;

            A.CallTo(() => _fakeNextMiddleware(_fakeHttpContext)).Throws(new Exception("fake exception"));

            await _middleware.InvokeAsync(_fakeHttpContext);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.That(problemDetails.Extensions["correlationId"].ToString(), Is.EqualTo("fakeCorrelationId"));
            Assert.That(_fakeHttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
            Assert.That(_fakeHttpContext.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(_fakeHttpContext.Response.Headers, Does.ContainKey(PermitServiceConstants.OriginHeaderKey).WithValue(PermitServiceConstants.PermitService));

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.UnhandledException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "fake exception").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExceptionIsOfTypePermitServiceException_ThenLogsErrorWithPermitServiceExceptionEventId()
        {
            var memoryStream = new MemoryStream();
            _fakeHttpContext.Request.Headers.Append(PermitServiceConstants.XCorrelationIdHeaderKey, "fakeCorrelationId");
            _fakeHttpContext.Response.Body = memoryStream;

            A.CallTo(() => _fakeNextMiddleware(_fakeHttpContext)).Throws(new PermitServiceException(EventIds.PermitServiceException.ToEventId(), "fakemessage"));

            await _middleware.InvokeAsync(_fakeHttpContext);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.That(problemDetails.Extensions["correlationId"].ToString(), Is.EqualTo("fakeCorrelationId"));
            Assert.That(_fakeHttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
            Assert.That(_fakeHttpContext.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(_fakeHttpContext.Response.Headers, Does.ContainKey(PermitServiceConstants.OriginHeaderKey).WithValue(PermitServiceConstants.PermitService));

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.PermitServiceException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "fakemessage").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExceptionIsOfTypeAesEncryptionException_ThenLogsErrorWithAesEncryptionExceptionEventId()
        {
            var memoryStream = new MemoryStream();
            _fakeHttpContext.Request.Headers.Append(PermitServiceConstants.XCorrelationIdHeaderKey, "fakeCorrelationId");
            _fakeHttpContext.Response.Body = memoryStream;

            A.CallTo(() => _fakeNextMiddleware(_fakeHttpContext)).Throws(new AesEncryptionException(EventIds.AesEncryptionException.ToEventId(), "fakemessage"));

            await _middleware.InvokeAsync(_fakeHttpContext);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.That(problemDetails.Extensions["correlationId"].ToString(), Is.EqualTo("fakeCorrelationId"));
            Assert.That(_fakeHttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
            Assert.That(_fakeHttpContext.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(_fakeHttpContext.Response.Headers, Does.ContainKey(PermitServiceConstants.OriginHeaderKey).WithValue(PermitServiceConstants.PermitService));

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.AesEncryptionException.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "fakemessage").MustHaveHappenedOnceExactly();
        }
    }
}