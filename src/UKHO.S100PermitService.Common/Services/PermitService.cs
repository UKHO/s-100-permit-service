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

        public async Task<HttpStatusCode> CreatePermitAsync(int licenceId, CancellationToken cancellationToken, string correlationId)
        {
            _logger.LogInformation(EventIds.CreatePermitStart.ToEventId(), "CreatePermit started");

            var userPermitServiceResponse = await _userPermitService.GetUserPermitAsync(licenceId, cancellationToken, correlationId);
            if(UserPermitServiceResponseValidator.IsResponseNull(userPermitServiceResponse))
            {
                _logger.LogWarning(EventIds.UserPermitServiceGetUserPermitsRequestCompletedWithNoContent.ToEventId(), "Request to UserPermitService responded with empty response");

                return HttpStatusCode.NoContent;
            }
            _userPermitService.ValidateUpnsAndChecksum(userPermitServiceResponse);

            var holdingsServiceResponse = await _holdingsService.GetHoldingsAsync(licenceId, cancellationToken, correlationId);
            if(ListExtensions.IsNullOrEmpty(holdingsServiceResponse))
            {
                _logger.LogWarning(EventIds.HoldingsServiceGetHoldingsRequestCompletedWithNoContent.ToEventId(), "Request to HoldingsService responded with empty response");

                return HttpStatusCode.NoContent;
            }

            var productKeyServiceRequest = ProductKeyServiceRequest(holdingsServiceResponse);

            var productKeys = await _productKeyService.GetProductKeysAsync(productKeyServiceRequest, cancellationToken, correlationId);

            var decryptedProductKeys = _s100Crypt.GetDecryptedKeysFromProductKeys(productKeys, _productKeyServiceApiConfiguration.Value.HardwareId);

            var productsList = GetProductsList();

            var listOfUpnInfo = _s100Crypt.GetDecryptedHardwareIdFromUserPermit(userPermitServiceResponse);

            foreach(var upnInfo in listOfUpnInfo)
            {
                CreatePermitXml(DateTimeOffset.Now, "AB", "ABC", upnInfo.Upn, "1.0", productsList);
            }

            _logger.LogInformation(EventIds.CreatePermitEnd.ToEventId(), "CreatePermit completed");

            return HttpStatusCode.OK;
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

        private static List<ProductKeyServiceRequest> ProductKeyServiceRequest(
            IEnumerable<HoldingsServiceResponse> holdingsServiceResponse) =>
            holdingsServiceResponse.SelectMany(x => x.Cells.Select(y => new ProductKeyServiceRequest
            {
                ProductName = y.CellCode,
                Edition = y.LatestEditionNumber
            })).ToList();
    }
}