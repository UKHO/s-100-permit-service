using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UKHO.S100PermitService.Common;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermitController : BaseController<PermitController>
    {
        private readonly ILogger<PermitController> _logger;
        private readonly IPermitService _permitService;
        private readonly IPermitReaderWriter _permitReaderWriter;
        private readonly IKeyVaultSecretService _keyVaultSecretService;

        public PermitController(IHttpContextAccessor httpContextAccessor,
                                    ILogger<PermitController> logger,
                                    IPermitService permitService,
                                    IPermitReaderWriter permitReaderWriter,
                                    IKeyVaultSecretService keyVaultSecretService)
        : base(httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permitService = permitService ?? throw new ArgumentNullException(nameof(permitService));
            _permitReaderWriter = permitReaderWriter ?? throw new ArgumentNullException(nameof(permitReaderWriter));
            _keyVaultSecretService = keyVaultSecretService ?? throw new ArgumentNullException(nameof(keyVaultSecretService));            
        }

        [HttpGet]
        [Route("/permits/{licenceId}")]
        [Authorize(Policy = PermitServiceConstants.PermitServicePolicy)]
        public virtual async Task<IActionResult> GeneratePermits(int licenceId)
        {
            _logger.LogInformation(EventIds.GeneratePermitStarted.ToEventId(), "Generate Permit API call started.");
              var response = _keyVaultSecretService.FetchSecret("80808");

            var productsList = new List<Products>();
            productsList.Add(new Products()
            {
                Id = "ID",
                DatasetPermit = new ProductsProductDatasetPermit[]
                {
                    new ProductsProductDatasetPermit() {
                        IssueDate = DateTimeOffset.Now.ToString("yyyy-MM-ddzzz"),
                        EditionNumber = 1,
                        EncryptedKey = "encryptedkey",
                        Expiry = DateTime.Now,
                        Filename = "filename",

                    }
                }
            });
            var upn = "ABCDEFGHIJKLMNOPQRSTUVYXYZ";

            _logger.LogInformation(EventIds.CreatePermitStart.ToEventId(), "CreatePermit call started");
            _permitService.CreatePermit(DateTimeOffset.Now, "AB", "ABC", upn, 1.0m, productsList);
            _logger.LogInformation(EventIds.CreatePermitEnd.ToEventId(), "CreatePermit call completed");

            await Task.CompletedTask;

            _logger.LogInformation(EventIds.GeneratePermitEnd.ToEventId(), "Generate Permit API call end.");

            return Ok();
        }
    }
}
