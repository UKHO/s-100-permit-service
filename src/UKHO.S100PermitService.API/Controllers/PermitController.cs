using Microsoft.AspNetCore.Mvc;
using UKHO.S100PermitService.Common.Enum;
using UKHO.S100PermitService.Common.Helpers;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API.Controllers
{
    [ApiController]
    public class PermitController : BaseController<PermitController>
    {
        private const string LicenceId = "/permits/{licenceId}";
        private readonly ILogger<PermitController> _logger;
        private readonly IPermitXmlService _permitXmlService;
        private readonly IXmlHelper _xmlHelper;
        private readonly IFileSystemHelper _fileSystemHelper;

        public PermitController(IHttpContextAccessor httpContextAccessor,
                                    ILogger<PermitController> logger,
                                    IPermitXmlService permitXmlService,
                                    IXmlHelper xmlHelper,
                                    IFileSystemHelper fileSystemHelper)
        : base(httpContextAccessor)
        {
            _logger = logger;
            _permitXmlService = permitXmlService;
            _xmlHelper = xmlHelper;
            _fileSystemHelper = fileSystemHelper;
        }

        [HttpPost]
        [Route(LicenceId)]
        public virtual async Task<IActionResult> GeneratePermits(int licenceId)
        {
            _logger.LogInformation(EventIds.GeneratePermitStart.ToEventId(), "User permit api call started.");

            var productsList = new List<products>();
            productsList.Add(new products()
            {
                id = "ID",
                datasetPermit = new productsProductDatasetPermit[]
                {
                    new productsProductDatasetPermit() {
                        issueDate = DateTimeOffset.Now.ToString("yyyy-MM-ddzzz"),
                        editionNumber = 1,
                        encryptedKey = "encryptedkey",
                        expiry = DateTime.Now,
                        filename = "filename",

                    }
                }
            });
            var upn = "ABCDEFGHIJKLMNOPQRSTUVYXYZ";
            var tempPath = Path.Combine(Path.GetTempPath(), "Master", $"PERMIT.xml");

            _logger.LogInformation(EventIds.GeneratePermitStart.ToEventId(), "Permit Map call started");
            var permit = _permitXmlService.MapPermit(DateTimeOffset.Now, "AB", "ABC", upn, 1.0m, productsList);
            _logger.LogInformation(EventIds.GeneratePermitStart.ToEventId(), "Permit Map call completed");

            _logger.LogInformation(EventIds.GeneratePermitStart.ToEventId(), "Permit Xml serialization started");
            var permitXml = _xmlHelper.GetPermitXmlString(permit);
            _logger.LogInformation(EventIds.GeneratePermitStart.ToEventId(), "Permit Xml serialization completed");

            _logger.LogInformation(EventIds.GeneratePermitStart.ToEventId(), "Xml file creation started");
            _fileSystemHelper.CreateFile(permitXml, tempPath);
            _logger.LogInformation(EventIds.GeneratePermitStart.ToEventId(), "Xml file creation completed");

            await Task.CompletedTask;
            _logger.LogInformation(EventIds.GeneratePermitEnd.ToEventId(), "User permit api call end.");
            return Ok();
        }

    }
}
