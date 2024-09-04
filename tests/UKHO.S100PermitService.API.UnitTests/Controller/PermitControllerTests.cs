using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.S100PermitService.API.Controllers;
using UKHO.S100PermitService.Common.Enum;

namespace UKHO.S100PermitService.API.UnitTests.Controller
{
    [TestFixture]
    public class PermitControllerTests
    {
        private PermitController _permitController;
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private ILogger<PermitController> _fakeLogger;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            A.CallTo(() => _fakeHttpContextAccessor.HttpContext).Returns(new DefaultHttpContext());
            _fakeLogger = A.Fake<ILogger<PermitController>>();
            _permitController = new PermitController(_fakeHttpContextAccessor, _fakeLogger);
        }

        [Test]
        public async Task WhenGetPermitIsCalledReturnsOKResponse()
        {
            var result = await _permitController.GeneratePermits(007);

            result.Equals(200);

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.GeneratePermitStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Generate Permit API call started | _X-Correlation-ID:{correlationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.GeneratePermitEnd.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Generate Permit API call end | _X-Correlation-ID:{correlationId}"
           ).MustHaveHappenedOnceExactly();
        }        
    }
}