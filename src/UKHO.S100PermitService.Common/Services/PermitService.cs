using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Extensions;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Validations;

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
        private readonly IOptions<ProductKeyServiceApiConfiguration> _productKeyServiceApiConfiguration;

        public PermitService(IPermitReaderWriter permitReaderWriter,
                             ILogger<PermitService> logger,
                             IHoldingsService holdingsService,
                             IUserPermitService userPermitService,
                             IProductKeyService productKeyService,
                             IS100Crypt s100Crypt,
                             IOptions<ProductKeyServiceApiConfiguration> productKeyServiceApiConfiguration)
        {
            _permitReaderWriter = permitReaderWriter ?? throw new ArgumentNullException(nameof(permitReaderWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsService = holdingsService ?? throw new ArgumentNullException(nameof(holdingsService));
            _userPermitService = userPermitService ?? throw new ArgumentNullException(nameof(userPermitService));
            _productKeyService = productKeyService ?? throw new ArgumentNullException(nameof(productKeyService));
            _s100Crypt = s100Crypt ?? throw new ArgumentNullException(nameof(s100Crypt));
            _productKeyServiceApiConfiguration = productKeyServiceApiConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceApiConfiguration));
        }

        public async Task<(HttpStatusCode, MemoryStream)> CreatePermitAsync(int licenceId, CancellationToken cancellationToken, string correlationId)
        {
            _logger.LogInformation(EventIds.CreatePermitStart.ToEventId(), "CreatePermit started");

            var userPermitServiceResponse = await _userPermitService.GetUserPermitAsync(licenceId, cancellationToken, correlationId);
            if(UserPermitServiceResponseValidator.IsResponseNull(userPermitServiceResponse))
            {
                _logger.LogWarning(EventIds.UserPermitServiceGetUserPermitsRequestCompletedWithNoContent.ToEventId(), "Request to UserPermitService responded with empty response");

                return (HttpStatusCode.NoContent, new MemoryStream());
            }
            _userPermitService.ValidateUpnsAndChecksum(userPermitServiceResponse);

            var holdingsServiceResponse = await _holdingsService.GetHoldingsAsync(licenceId, cancellationToken, correlationId);
            if(ListExtensions.IsNullOrEmpty(holdingsServiceResponse))
            {
                _logger.LogWarning(EventIds.HoldingsServiceGetHoldingsRequestCompletedWithNoContent.ToEventId(), "Request to HoldingsService responded with empty response");

                return (HttpStatusCode.NoContent, new MemoryStream());
            }

            var productKeyServiceRequest = ProductKeyServiceRequest(holdingsServiceResponse);

            var productKeys = await _productKeyService.GetProductKeysAsync(productKeyServiceRequest, cancellationToken, correlationId);

            var decryptedProductKeys = _s100Crypt.GetDecryptedKeysFromProductKeys(productKeys, _productKeyServiceApiConfiguration.Value.HardwareId);

            var listOfUpnInfo = _s100Crypt.GetDecryptedHardwareIdFromUserPermit(userPermitServiceResponse);

            var productsList = new List<Products>();
            productsList.AddRange(GetProductsList());

            var permits = new List<Permit>();

            foreach(var upnInfo in listOfUpnInfo)
            {
                permits.Add(new Permit
                {
                    Header = new Header
                    {
                        IssueDate = DateTimeOffset.Now.ToString(DateFormat),
                        DataServerIdentifier = "GB00",
                        DataServerName = "UK Hydrographic Office",
                        Userpermit = upnInfo.Upn,
                        Version = "1.0",
                    },
                    Products = [.. productsList],
                    Title = upnInfo.Title,
                });
            };

            var permitDetails = CreatePermits(permits);

            _logger.LogInformation(EventIds.CreatePermitEnd.ToEventId(), "CreatePermit completed");

            return (HttpStatusCode.OK, permitDetails);
        }

        private MemoryStream CreatePermits(List<Permit> permits)
        {
            _logger.LogInformation(EventIds.FileCreationStart.ToEventId(), "Permit Xml file creation started");

            var permitDetails = _permitReaderWriter.CreatePermits(permits);

            if(permitDetails.Length > 0)
            {
                _logger.LogInformation(EventIds.FileCreationEnd.ToEventId(), "Permit Xml file creation completed");
            }
            else
            {
                _logger.LogError(EventIds.EmptyPermitXml.ToEventId(), "Empty permit xml is received");
            }
            return permitDetails;
        }

        [ExcludeFromCodeCoverage]
        private static List<Products> GetProductsList()
        {
            var productsList = new List<Products>
            {
                new()
                {
                    Id = "ID1",
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
                },new()
                {
                    Id = "ID2",
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

        private static List<ProductKeyServiceRequest> ProductKeyServiceRequest(
            IEnumerable<HoldingsServiceResponse> holdingsServiceResponse) =>
            holdingsServiceResponse.SelectMany(x => x.Cells.Select(y => new ProductKeyServiceRequest
            {
                ProductName = y.CellCode,
                Edition = y.LatestEditionNumber
            })).ToList();
    }
}