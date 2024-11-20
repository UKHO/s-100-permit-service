using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Text;
using UKHO.S100PermitService.API.Controllers;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models;
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
        public async Task WhenPermitGeneratedSuccessfully_ThenReturnsZipStreamResponse()
        {
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(GetExpectedXmlString()));

            A.CallTo(() => _fakePermitService.ProcessPermitRequestAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                            .Returns(PermitServiceResult.Success(expectedStream));

            var result = await _permitController.GeneratePermits(007);

            result.Should().BeOfType<FileStreamResult>();
            ((FileStreamResult)result).FileDownloadName.Should().Be("Permits.zip");
            ((FileStreamResult)result).FileStream.Length.Should().Be(expectedStream.Length);

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.GeneratePermitStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "GeneratePermit API call started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
           call.Method.Name == "Log"
           && call.GetArgument<LogLevel>(0) == LogLevel.Information
           && call.GetArgument<EventId>(1) == EventIds.GeneratePermitCompleted.ToEventId()
           && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "GeneratePermit API call completed."
           ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.NoContent)]
        [TestCase(HttpStatusCode.InternalServerError)]
        public async Task WhenPermitGenerationFailed_ThenReturnsNotOkResponse(HttpStatusCode httpStatusCode)
        {
            A.CallTo(() => _fakePermitService.ProcessPermitRequestAsync(A<int>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetPermitServiceResult(httpStatusCode));

            var result = await _permitController.GeneratePermits(1);

            switch(result)
            {
                case StatusCodeResult statusCodeResult when statusCodeResult.StatusCode == (int)httpStatusCode: // 500: InternalServerError
                    statusCodeResult.StatusCode.Should().Be((int)httpStatusCode);
                    break;

                case BadRequestObjectResult badRequestObjectResult when badRequestObjectResult.StatusCode == (int)httpStatusCode: //400: BadRequest
                    badRequestObjectResult.StatusCode.Should().Be((int)httpStatusCode);
                    badRequestObjectResult.Value.Should().BeEquivalentTo(new
                    {
                        Errors = new List<ErrorDetail>
                    {
                        new() { Description = "LicenceId is incorrect", Source = "GetUserPermits" }
                    }
                    });
                    break;

                case NotFoundObjectResult notFoundObjectResult when notFoundObjectResult.StatusCode == (int)httpStatusCode: //404: NotFound
                    notFoundObjectResult.StatusCode.Should().Be((int)httpStatusCode);
                    notFoundObjectResult.Value.Should().BeEquivalentTo(new
                    {
                        Errors = new List<ErrorDetail>
                    {
                        new() { Description = "Licence Not Found", Source = "GetUserPermits" }
                    }
                    });
                    break;

                case NoContentResult noContentResult when noContentResult.StatusCode == (int)httpStatusCode: // 204: NoContent
                    noContentResult.StatusCode.Should().Be((int)httpStatusCode);
                    break;

            }

            A.CallTo(_fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Information
                 && call.GetArgument<EventId>(1) == EventIds.GeneratePermitStarted.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "GeneratePermit API call started."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GeneratePermitCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "GeneratePermit API call completed."
            ).MustHaveHappenedOnceExactly();
        }

        private PermitServiceResult GetPermitServiceResult(HttpStatusCode httpStatusCode)
        {
            return httpStatusCode switch
            {
                HttpStatusCode.BadRequest => PermitServiceResult.BadRequest(new ErrorResponse() { CorrelationId = Guid.NewGuid().ToString(), Errors = [new ErrorDetail() { Description = "LicenceId is incorrect", Source = "GetUserPermits" }] }),
                HttpStatusCode.NotFound => PermitServiceResult.NotFound(new ErrorResponse() { CorrelationId = Guid.NewGuid().ToString(), Errors = [new ErrorDetail() { Description = "Licence Not Found", Source = "GetUserPermits" }] }),
                HttpStatusCode.NoContent => PermitServiceResult.NoContent(),
                HttpStatusCode.InternalServerError => PermitServiceResult.InternalServerError()
            };
        }

        private string GetExpectedXmlString()
        {
            var expectedResult = "<?xmlversion=\"1.0\"encoding=\"UTF-8\"standalone=\"yes\"?><Permitxmlns:S100SE=\"http://www.iho.int/s100/se/5.2\"xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\"xmlns=\"http://www.iho.int/s100/se/5.2\"><S100SE:header>";
            expectedResult += "<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName><S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier><S100SE:version>1</S100SE:version>";
            expectedResult += "<S100SE:userpermit>fakeUserPermit</S100SE:userpermit></S100SE:header><S100SE:products><S100SE:productid=\"fakeID\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename><S100SE:editionNumber>1</S100SE:editionNumber>";
            expectedResult += "<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:expiry>2024-09-02</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product></S100SE:products></Permit>";

            return expectedResult;
        }
    }
}