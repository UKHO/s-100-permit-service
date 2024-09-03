using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.S100PermitService.API.Controllers;
using UKHO.S100PermitService.Common.Enum;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

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
            var result = (OkObjectResult)await _permitController.GeneratePermits(007);
          
            Assert.AreEqual(200, result.StatusCode);
            A.CallTo(_fakeLogger).Where(call =>
                     call.Method.Name == "Log"
                     && call.GetArgument<LogLevel>(0) == LogLevel.Information
                     && call.GetArgument<EventId>(1) == EventIds.GeneratePermitStart.ToEventId()
                      && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString()
                     == "User permit api call started.Correlation-ID _X-Correlation-ID : {CorrelationId}"
                     ).MustHaveHappenedOnceExactly();           
        }
    }
}