using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Text;
using UKHO.S100PermitService.API.Controllers;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.Request;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API.UnitTests.Controller
{
    [TestFixture]
    public class PermitControllerTests
    {
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private ILogger<PermitController> _fakeLogger;
        private IPermitService _fakePermitService;
        private DefaultHttpContext _fakeHttpContext;

        private PermitController _permitController;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            _fakeLogger = A.Fake<ILogger<PermitController>>();
            _fakePermitService = A.Fake<IPermitService>();
            _fakeHttpContext = new DefaultHttpContext();
            A.CallTo(() => _fakeHttpContextAccessor.HttpContext).Returns(_fakeHttpContext);

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
            var permitRequest = new PermitRequest
            {
                Products = new List<Product>
                {
                    new() {
                        ProductName = "Product1",
                        EditionNumber = 1,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(1).ToString("YYYY-MM-DD")
                    },
                    new() {
                        ProductName = "Product2",
                        EditionNumber = 2,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(1).ToString("YYYY-MM-DD")
                    }
                },
                UserPermits = new List<UserPermit>
                {
                    new() {
                        Title = "IHO Test System",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                    },
                    new() {
                        Title = "OeM Test 1",
                        Upn = "7B5CED73389DECDB110E6E803F957253F0DE13D1G7H8I9"
                    }
                }
            };

            A.CallTo(() => _fakePermitService.ProcessPermitRequestAsync(A<PermitRequest>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                                .Returns(PermitServiceResult.Success(expectedStream));

            var result = await _permitController.GenerateS100Permits(permitRequest);

            result.Should().BeOfType<FileStreamResult>();
            var fileStreamResult = (FileStreamResult)result;
            fileStreamResult.FileDownloadName.Should().Be("Permits.zip");
            fileStreamResult.FileStream.Length.Should().Be(expectedStream.Length);
            _fakeHttpContext.Response.Headers.Should().ContainKey(PermitServiceConstants.OriginHeaderKey).WhoseValue.Should().Equal(PermitServiceConstants.PermitService);

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log" &&
                call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.GeneratePermitStarted.ToEventId() &&
                call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "GeneratePermit API call started for ProductType {productType}.")
                .MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log" &&
                call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                call.GetArgument<EventId>(1) == EventIds.GeneratePermitCompleted.ToEventId() &&
                call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "GeneratePermit API call completed for ProductType {productType}.")
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.ServiceUnavailable)]
        [TestCase(HttpStatusCode.UnsupportedMediaType)]
        [TestCase(HttpStatusCode.InternalServerError)]
        public async Task WhenPermitGenerationFailed_ThenReturnsNotOkResponseWithOriginHeader(HttpStatusCode httpStatusCode)
        {
            var permitRequest = new PermitRequest();
            A.CallTo(() => _fakePermitService.ProcessPermitRequestAsync(A<PermitRequest>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(GetPermitServiceResult(httpStatusCode));

            var result = await _permitController.GenerateS100Permits(permitRequest);

            switch(result)
            {
                case StatusCodeResult statusCodeResult when statusCodeResult.StatusCode == (int)httpStatusCode:
                    statusCodeResult.StatusCode.Should().Be((int)httpStatusCode);
                    _fakeHttpContext.Response.Headers.Should().ContainKey(PermitServiceConstants.OriginHeaderKey).WhoseValue.Should().Equal(PermitServiceConstants.PermitService);
                    break;

                case BadRequestObjectResult badRequestObjectResult when badRequestObjectResult.StatusCode == (int)httpStatusCode: //400: BadRequest
                    badRequestObjectResult.StatusCode.Should().Be((int)httpStatusCode);
                    badRequestObjectResult.Value.Should().BeEquivalentTo(new { Errors = new List<ErrorDetail> { new() { Description = "Key not found for ProductName: Product1 and Edition: 1.", Source = "GetProductKey" } } });
                    _fakeHttpContext.Response.Headers.Should().ContainKey(PermitServiceConstants.OriginHeaderKey).WhoseValue.Should().Equal(PermitServiceConstants.ProductKeyService);
                    break;
            }

            A.CallTo(_fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Information
                 && call.GetArgument<EventId>(1) == EventIds.GeneratePermitStarted.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "GeneratePermit API call started for ProductType {productType}."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.GeneratePermitCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "GeneratePermit API call completed for ProductType {productType}."
            ).MustHaveHappenedOnceExactly();
        }

        private PermitServiceResult GetPermitServiceResult(HttpStatusCode httpStatusCode)
        {
            return httpStatusCode switch
            {
                HttpStatusCode.BadRequest => PermitServiceResult.Failure(httpStatusCode, PermitServiceConstants.ProductKeyService, new ErrorResponse
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Errors = [new ErrorDetail
                                {
                                    Description = "Key not found for ProductName: Product1 and Edition: 1.",
                                    Source = "GetProductKey"
                                }]
                }),
                _ => PermitServiceResult.Failure(httpStatusCode, PermitServiceConstants.PermitService, new ErrorResponse { CorrelationId = Guid.NewGuid().ToString() })
            };
        }

        private static string GetExpectedXmlString()
        {
            var sb = new StringBuilder();
            sb.Append("<?xmlversion=\"1.0\"encoding=\"UTF-8\"standalone=\"yes\"?><Permitxmlns:S100SE=\"http://www.iho.int/s100/se/5.2\"xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\"xmlns=\"http://www.iho.int/s100/se/5.2\"><S100SE:header>");
            sb.Append("<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName><S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier><S100SE:version>1</S100SE:version>");
            sb.Append("<S100SE:userpermit>fakeUserPermit</S100SE:userpermit></S100SE:header><S100SE:products><S100SE:productid=\"fakeID\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename><S100SE:editionNumber>1</S100SE:editionNumber>");
            sb.Append("<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:expiry>2024-09-02</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product></S100SE:products></Permit>");

            return sb.ToString();
        }
    }
}