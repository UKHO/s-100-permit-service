using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.S100PermitService.API.Controllers;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API.UnitTests.Controller
{
    [TestFixture]
    public class PermitControllerTests
    {
        private PermitController _permitController;
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private ILogger<PermitController> _fakeLogger;
        private IPermitXmlService _fakePermitXmlService;
        private IXmlHelper _fakeXmlHelper;
        private IFileSystemHelper _fakeFileSystemHelper;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();            
            _fakeLogger = A.Fake<ILogger<PermitController>>();
            _fakePermitXmlService = A.Fake<IPermitXmlService>();
            _fakeXmlHelper = A.Fake<IXmlHelper>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            _permitController = new PermitController(_fakeHttpContextAccessor, _fakeLogger,_fakePermitXmlService,_fakeXmlHelper,_fakeFileSystemHelper);
        }

        [Test]
        public async Task WhenGetPermitIsCalled_ThenReturnsOKResponse()
        {
            var result = (OkResult)await _permitController.GeneratePermits(007);
            
            result.StatusCode.Should().Be(StatusCodes.Status200OK);

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