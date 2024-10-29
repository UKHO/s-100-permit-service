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
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Validations;

namespace UKHO.S100PermitService.Common.Services
{
    public class PermitService : IPermitService
    {
        private const string DateFormat = "yyyy-MM-ddzzz";
        private readonly string _issueDate = DateTimeOffset.UtcNow.ToString(DateFormat);

        private readonly ILogger<PermitService> _logger;
        private readonly IPermitReaderWriter _permitReaderWriter;
        private readonly IHoldingsService _holdingsService;
        private readonly IUserPermitService _userPermitService;
        private readonly IProductKeyService _productKeyService;
        private readonly IOptions<PermitFileConfiguration> _permitFileConfiguration;
        private readonly IS100Crypt _s100Crypt;
        private readonly IOptions<ProductKeyServiceApiConfiguration> _productKeyServiceApiConfiguration;

        public PermitService(IPermitReaderWriter permitReaderWriter,
                             ILogger<PermitService> logger,
                             IHoldingsService holdingsService,
                             IUserPermitService userPermitService,
                             IProductKeyService productKeyService,
                             IS100Crypt s100Crypt,
                             IOptions<ProductKeyServiceApiConfiguration> productKeyServiceApiConfiguration,
                             IOptions<PermitFileConfiguration> permitFileConfiguration)
        {
            _permitReaderWriter = permitReaderWriter ?? throw new ArgumentNullException(nameof(permitReaderWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsService = holdingsService ?? throw new ArgumentNullException(nameof(holdingsService));
            _userPermitService = userPermitService ?? throw new ArgumentNullException(nameof(userPermitService));
            _productKeyService = productKeyService ?? throw new ArgumentNullException(nameof(productKeyService));
            _s100Crypt = s100Crypt ?? throw new ArgumentNullException(nameof(s100Crypt));
            _productKeyServiceApiConfiguration = productKeyServiceApiConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceApiConfiguration));
            _permitFileConfiguration = permitFileConfiguration ?? throw new ArgumentNullException(nameof(permitFileConfiguration));
        }

        public async Task<(HttpStatusCode httpStatusCode, Stream stream)> ProcessPermitRequestAsync(int licenceId, CancellationToken cancellationToken, string correlationId)
        {
            _logger.LogInformation(EventIds.ProcessPermitRequestStarted.ToEventId(), "Process permit request started.");

            var (userPermitStatusCode, userPermitServiceResponse) = await _userPermitService.GetUserPermitAsync(licenceId, cancellationToken, correlationId);
            if(UserPermitServiceResponseValidator.IsResponseNull(userPermitServiceResponse))
            {
                if(userPermitStatusCode == HttpStatusCode.NotFound)
                {
                    return (HttpStatusCode.NotFound, new MemoryStream());
                }

                _logger.LogWarning(EventIds.UserPermitServiceGetUserPermitsRequestCompletedWithNoContent.ToEventId(), "Request to UserPermitService responded with empty response.");

                return (HttpStatusCode.NoContent, new MemoryStream());
            }
            _userPermitService.ValidateUpnsAndChecksum(userPermitServiceResponse);

            var (holdingsStatusCode, holdingsServiceResponse) = await _holdingsService.GetHoldingsAsync(licenceId, cancellationToken, correlationId);
            if(ListExtensions.IsNullOrEmpty(holdingsServiceResponse))
            {
                if(holdingsStatusCode == HttpStatusCode.NotFound)
                {
                    return (HttpStatusCode.NotFound, new MemoryStream());
                }

                _logger.LogWarning(EventIds.HoldingsServiceGetHoldingsRequestCompletedWithNoContent.ToEventId(), "Request to HoldingsService responded with empty response.");

                return (HttpStatusCode.NoContent, new MemoryStream());
            }

            var holdingsWithLatestExpiry = _holdingsService.FilterHoldingsByLatestExpiry(holdingsServiceResponse);

            var productKeyServiceRequest = CreateProductKeyServiceRequest(holdingsWithLatestExpiry);

            var productKeys = await _productKeyService.GetProductKeysAsync(productKeyServiceRequest, cancellationToken, correlationId);

            var decryptedProductKeys = _s100Crypt.GetDecryptedKeysFromProductKeys(productKeys, _productKeyServiceApiConfiguration.Value.HardwareId);

            var listOfUpnInfo = _s100Crypt.GetDecryptedHardwareIdFromUserPermit(userPermitServiceResponse);

            var permitDetails = BuildPermits(holdingsWithLatestExpiry, decryptedProductKeys, listOfUpnInfo);

            _logger.LogInformation(EventIds.ProcessPermitRequestCompleted.ToEventId(), "Process permit request completed.");

            return (HttpStatusCode.OK, permitDetails);
        }

        private Stream BuildPermits(IEnumerable<HoldingsServiceResponse> holdingsServiceResponses, IEnumerable<ProductKey> decryptedProductKeys, IEnumerable<UpnInfo> upnInfos)
        {
            var permitDictionary = new Dictionary<string, Permit>();
            var xsdVersion = _permitReaderWriter.ReadXsdVersion();

            foreach(var upnInfo in upnInfos)
            {
                var productsList = GetProductsList(holdingsServiceResponses, decryptedProductKeys, upnInfo.DecryptedHardwareId);

                var permit = new Permit
                {
                    Header = new Header
                    {
                        IssueDate = _issueDate,
                        DataServerIdentifier = _permitFileConfiguration.Value.DataServerIdentifier,
                        DataServerName = _permitFileConfiguration.Value.DataServerName,
                        Userpermit = upnInfo.Upn,
                        Version = xsdVersion
                    },
                    Products = [.. productsList]
                };

                permitDictionary.Add(upnInfo.Title, permit);
            }

            var permitDetails = _permitReaderWriter.CreatePermitZip(permitDictionary);

            return permitDetails;
        }

        [ExcludeFromCodeCoverage]
        private IEnumerable<Products> GetProductsList(IEnumerable<HoldingsServiceResponse> holdingsServiceResponse, IEnumerable<ProductKey> decryptedProductKeys, string hardwareId)
        {
            var productsList = new List<Products>();
            var products = new Products();

            foreach(var holding in holdingsServiceResponse)
            {
                foreach(var cell in holding.Cells.OrderBy(x => x.CellCode))
                {
                    products.Id = $"S-{cell.CellCode[..3]}";

                    var dataPermit = new ProductsProductDatasetPermit
                    {
                        EditionNumber = byte.Parse(cell.LatestEditionNumber),
                        EncryptedKey = GetEncryptedKey(decryptedProductKeys, hardwareId, cell.CellCode),
                        Filename = cell.CellCode,
                        Expiry = holding.ExpiryDate
                    };

                    if(productsList.Any(x => x.Id == products.Id))
                    {
                        productsList.FirstOrDefault(x => x.Id == products.Id).DatasetPermit.Add(dataPermit);
                    }
                    else
                    {
                        products.DatasetPermit = new List<ProductsProductDatasetPermit> { dataPermit };
                        productsList.Add(products);
                    }
                    products = new();
                }
            }
            return productsList;
        }

        private List<ProductKeyServiceRequest> CreateProductKeyServiceRequest(
            IEnumerable<HoldingsServiceResponse> holdingsServiceResponse) =>
            holdingsServiceResponse.SelectMany(x => x.Cells.Select(y => new ProductKeyServiceRequest
            {
                ProductName = y.CellCode,
                Edition = y.LatestEditionNumber
            })).ToList();

        private string GetEncryptedKey(IEnumerable<ProductKey> decryptedProductKeys, string hardwareId, string cellCode)
        {
            var decryptedProductKey = decryptedProductKeys.FirstOrDefault(pk => pk.ProductName == cellCode).DecryptedKey;

            return _s100Crypt.CreateEncryptedKey(decryptedProductKey, hardwareId);
        }
    }
}