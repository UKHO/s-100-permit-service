using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Validation;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.Services
{
    public class PermitService : IPermitService
    {
        private const string DateFormat = "yyyy-MM-ddzzz";

        private readonly ILogger<PermitService> _logger;
        private readonly IPermitReaderWriter _permitReaderWriter;
        private readonly IHoldingsService _holdingsService;
        private readonly IUserPermitService _userPermitService;
        private readonly IProductKeyService _productKeyService;
        private readonly IS100Crypt _s100Crypt;
        private readonly IUserPermitValidator _userPermitValidator;

        public PermitService(IPermitReaderWriter permitReaderWriter,
                                ILogger<PermitService> logger,
                                IHoldingsService holdingsService,
                                IUserPermitService userPermitService,
                                IProductKeyService productKeyService,
                                IS100Crypt s100Crypt,
            IUserPermitValidator userPermitValidator)
        {
            _permitReaderWriter = permitReaderWriter ?? throw new ArgumentNullException(nameof(permitReaderWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsService = holdingsService ?? throw new ArgumentNullException(nameof(holdingsService));
            _userPermitService = userPermitService ?? throw new ArgumentNullException(nameof(userPermitService));
            _productKeyService = productKeyService ?? throw new ArgumentNullException(nameof(productKeyService));
            _s100Crypt = s100Crypt ?? throw new ArgumentNullException(nameof(s100Crypt));
            _userPermitValidator = userPermitValidator ?? throw new ArgumentNullException(nameof(userPermitValidator));
        }

        public async Task CreatePermitAsync(int licenceId, CancellationToken cancellationToken, string correlationId)
        {
            _logger.LogInformation(EventIds.CreatePermitStart.ToEventId(), "CreatePermit started");

            var userPermitServiceResponse = await _userPermitService.GetUserPermitAsync(licenceId, cancellationToken, correlationId);

            if(ValidateUpnsAndChecksumAsync(userPermitServiceResponse))
            {
                var holdingsServiceResponse = await _holdingsService.GetHoldingsAsync(licenceId, cancellationToken, correlationId);

                var productsList = GetProductsList();

                var productKeyServiceRequest = ProductKeyServiceRequest(holdingsServiceResponse);

                var pksResponseData = await _productKeyService.GetPermitKeysAsync(productKeyServiceRequest, cancellationToken, correlationId);

                foreach(var userPermits in userPermitServiceResponse.UserPermits)
                {
                    var decryptedHardwareId = _s100Crypt.GetHwIdFromUserPermit(userPermits.Upn);

                    CreatePermitXml(DateTimeOffset.Now, "AB", "ABC", userPermits.Upn, "1.0", productsList);
                }

                _logger.LogInformation(EventIds.CreatePermitEnd.ToEventId(), "CreatePermit completed");
            }
        }

        private void CreatePermitXml(DateTimeOffset issueDate, string dataServerIdentifier, string dataServerName, string userPermit, string version, List<Products> products)
        {
            var productsList = new List<Products>();
            productsList.AddRange(products);
            var permit = new Permit
            {
                Header = new Header
                {
                    IssueDate = issueDate.ToString(DateFormat),
                    DataServerIdentifier = dataServerIdentifier,
                    DataServerName = dataServerName,
                    Userpermit = userPermit,
                    Version = version
                },
                Products = [.. productsList]
            };

            _logger.LogInformation(EventIds.XmlSerializationStart.ToEventId(), "Permit Xml serialization started");
            var permitXml = _permitReaderWriter.ReadPermit(permit);
            _logger.LogInformation(EventIds.XmlSerializationEnd.ToEventId(), "Permit Xml serialization completed");

            if(!string.IsNullOrEmpty(permitXml))
            {
                _permitReaderWriter.WritePermit(permitXml);
                _logger.LogInformation(EventIds.FileCreationEnd.ToEventId(), "Permit Xml file created");
            }
            else
            {
                _logger.LogError(EventIds.EmptyPermitXml.ToEventId(), "Empty permit xml is received");
            }
        }

        [ExcludeFromCodeCoverage]
        private static List<Products> GetProductsList()
        {
            var productsList = new List<Products>
            {
                new()
                {
                    Id = "ID",
                    DatasetPermit =
                    [
                        new() {
                            IssueDate = DateTimeOffset.Now.ToString("yyyy-MM-ddzzz"),
                            EditionNumber = 1,
                            EncryptedKey = "encryptedkey",
                            Expiry = DateTime.Now,
                            Filename = "filename",

                        }
                    ]
                }
            };
            return productsList;
        }

        private static List<ProductKeyServiceRequest> ProductKeyServiceRequest(List<HoldingsServiceResponse> holdingsServiceResponse) =>
             holdingsServiceResponse.SelectMany(x => x.Cells.Select(y => new ProductKeyServiceRequest
             {
                 ProductName = y.CellCode,
                 Edition = y.LatestEditionNumber
             })).ToList();

        private bool ValidateUpnsAndChecksumAsync(UserPermitServiceResponse userPermitServiceResponse)
        {
            var result = _userPermitValidator.Validate(userPermitServiceResponse);
            if(result.IsValid)
            {
                return true;
            }
            var errorMessages = result.Errors.GroupBy(item => item.ErrorMessage)
                .Select(group => new
                {
                    Errors = string.Join(", ", group.Key)
                });

            var errorMessage = string.Join(", ", errorMessages
                .Select(group => group.Errors)
                .Distinct());

            throw new PermitServiceException(EventIds.UpnLengthOrChecksumValidationFailed.ToEventId(), errorMessage);
        }
    }
}