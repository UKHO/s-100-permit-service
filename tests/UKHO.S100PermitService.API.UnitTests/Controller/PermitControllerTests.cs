using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using UKHO.S100PermitService.API.Controllers;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API.UnitTests.Controller
{
    [TestFixture]
    public class PermitControllerTests
    {
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private ILogger<PermitController> _fakeLogger;
        private IPermitService _fakePermitService;

        private PermitController _permitController;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            _fakeLogger = A.Fake<ILogger<PermitController>>();
            _fakePermitService = A.Fake<IPermitService>();

            _permitController = new PermitController(_fakeHttpContextAccessor, _fakeLogger, _fakePermitService);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new PermitController(_fakeHttpContextAccessor, null, _fakePermitService);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullPermitService = () => new PermitController(_fakeHttpContextAccessor, _fakeLogger, null);
            nullPermitService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitService");
        }

        [Test]
        public async Task WhenGetPermitIsCalled_ThenReturnsOKResponse()
        {
            A.CallTo(() => _fakePermitService.CreatePermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(HttpStatusCode.OK);

            var result = (OkResult)await _permitController.GeneratePermits(007);

            result.StatusCode.Should().Be(StatusCodes.Status200OK);

            A.CallTo(() => _fakePermitService.CreatePermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.GeneratePermitStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Generate Permit API call started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.GeneratePermitEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Generate Permit API call end."
           ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenGetPermitIsCalled_ThenReturnsNoContentResponse()
        {
            A.CallTo(() => _fakePermitService.CreatePermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored))
                .Returns(HttpStatusCode.NoContent);

            var result = (StatusCodeResult)await _permitController.GeneratePermits(1);

            result.StatusCode.Should().Be(StatusCodes.Status204NoContent);

            A.CallTo(() => _fakePermitService.CreatePermitAsync(A<int>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored)).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GeneratePermitStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Generate Permit API call started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GeneratePermitEnd.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Generate Permit API call end."
            ).MustHaveHappenedOnceExactly();
        }
    }
}