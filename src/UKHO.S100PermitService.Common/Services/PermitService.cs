using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.Request;

namespace UKHO.S100PermitService.Common.Services
{
    public class PermitService : IPermitService
    {
        private const string DateFormat = "yyyy-MM-ddzzz";
        private readonly string _issueDate = DateTimeOffset.UtcNow.ToString(DateFormat);

        private readonly ILogger<PermitService> _logger;
        private readonly IPermitReaderWriter _permitReaderWriter;
        private readonly IProductKeyService _productKeyService;
        private readonly IS100Crypt _s100Crypt;
        private readonly IOptions<PermitFileConfiguration> _permitFileConfiguration;
        private readonly IOptions<ProductKeyServiceApiConfiguration> _productKeyServiceApiConfiguration;

        public PermitService(IPermitReaderWriter permitReaderWriter,
                             ILogger<PermitService> logger,
                             IProductKeyService productKeyService,
                             IS100Crypt s100Crypt,
                             IOptions<ProductKeyServiceApiConfiguration> productKeyServiceApiConfiguration,
                             IOptions<PermitFileConfiguration> permitFileConfiguration)
        {
            _permitReaderWriter = permitReaderWriter ?? throw new ArgumentNullException(nameof(permitReaderWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _productKeyService = productKeyService ?? throw new ArgumentNullException(nameof(productKeyService));
            _s100Crypt = s100Crypt ?? throw new ArgumentNullException(nameof(s100Crypt));
            _productKeyServiceApiConfiguration = productKeyServiceApiConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceApiConfiguration));
            _permitFileConfiguration = permitFileConfiguration ?? throw new ArgumentNullException(nameof(permitFileConfiguration));
        }

        /// <summary>
        /// Get required data from dependent services and build zip stream containing PERMIT.XML.
        /// </summary>
        /// <remarks>
        /// If duplicate holdings data found, Then remove duplicate dataset and select the dataset with highest expiry date.
        /// </remarks>
        /// <param name="productType">Requested product type.</param>
        /// <param name="permitRequest">The JSON body containing products and UPNs.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <response code="200">Zip stream containing PERMIT.XML.</response>
        /// <response code="400,401,403,500">If service responded with other than 200 Ok StatusCode, Then errorResponse will be return with origin PKS</response>
        /// <response code="500">InternalServerError - exception occurred.</response>
        public async Task<PermitServiceResult> ProcessPermitRequestAsync(string productType, PermitRequest permitRequest, string correlationId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(EventIds.ProcessPermitRequestStarted.ToEventId(), "Process permit request started for ProductType {productType}.", productType);

            var productKeyServiceRequest = CreateProductKeyServiceRequest(permitRequest.Products);

            var productKeyServiceResponseResult = await _productKeyService.GetProductKeysAsync(productKeyServiceRequest, correlationId, cancellationToken);

            if(productKeyServiceResponseResult.IsSuccess)
            {
                var decryptedProductKeys = await _s100Crypt.GetDecryptedKeysFromProductKeysAsync(productKeyServiceResponseResult.Value, _productKeyServiceApiConfiguration.Value.HardwareId);

                var listOfUpnInfo = await _s100Crypt.GetDecryptedHardwareIdFromUserPermitAsync(permitRequest.UserPermits);

                var permitDetails = await BuildPermitsAsync(permitRequest.Products, decryptedProductKeys, listOfUpnInfo);

                _logger.LogInformation(EventIds.ProcessPermitRequestCompleted.ToEventId(), "Process permit request completed for ProductType {productType}.", productType);

                return PermitServiceResult.Success(permitDetails);
            }

            return PermitServiceResult.Failure(productKeyServiceResponseResult.StatusCode, productKeyServiceResponseResult.ErrorResponse);
        }

        /// <summary>
        /// Build zip stream containing PERMIT.XML.
        /// </summary>
        /// <remarks>
        /// Generate PERMIT.XML for the respective User Permit Number (UPN) and provides the zip stream containing all the PERMIT.XML.
        /// If any exception occurred, Then PermitServiceException exception will be thrown.
        /// </remarks>
        /// <param name="productsDetails">Products details.</param>
        /// <param name="decryptedProductKeys">Decrypted keys from product Key with well known hardware id.</param>
        /// <param name="upnInfos">User Permit Numbers (UPN) and DecryptedHardwareIds(HW_ID) from EncryptedHardwareIds(Part of UPN) with MKeys.</param>
        /// <returns>Zip stream containing PERMIT.XML.</returns>
        private async Task<Stream> BuildPermitsAsync(IEnumerable<Product> productsDetails, IEnumerable<ProductKey> decryptedProductKeys, IEnumerable<UpnInfo> upnInfos)
        {
            var permitDictionary = new Dictionary<string, Permit>();
            var xsdVersion = _permitReaderWriter.ReadXsdVersion();

            foreach(var upnInfo in upnInfos)
            {
                var productsList = await GetProductsListAsync(productsDetails, decryptedProductKeys, upnInfo.DecryptedHardwareId);

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
            var permitDetails = await _permitReaderWriter.CreatePermitZipAsync(permitDictionary);
            return permitDetails;
        }

        /// <summary>
        /// Get product details from Products and ProductKeyService
        /// </summary>
        /// <param name="productsDetails">Products details.</param>
        /// <param name="decryptedProductKeys">Decrypted keys from product Key with well known hardware id.</param>
        /// <param name="hardwareId">Decrypted HW_ID from Upn.</param>
        /// <returns>Products</returns>
        private async Task<IEnumerable<Products>> GetProductsListAsync(IEnumerable<Product> productsDetails, IEnumerable<ProductKey> decryptedProductKeys, string hardwareId)
        {
            var productsList = new List<Products>();
            var products = new Products();

            foreach(var cell in productsDetails.OrderBy(x => x.ProductName))
            {
                products.Id = $"S-{cell.ProductName[..3]}";

                var dataPermit = new ProductsProductDatasetPermit
                {
                    EditionNumber = byte.Parse(cell.EditionNumber.ToString()),
                    EncryptedKey = await GetEncryptedKeyAsync(decryptedProductKeys, hardwareId, cell.ProductName),
                    Filename = cell.ProductName,
                    Expiry = cell.PermitExpiryDate
                };

                if(productsList.Any(x => x.Id == products.Id))
                {
                    productsList.FirstOrDefault(x => x.Id == products.Id).DatasetPermit.Add(dataPermit);
                }
                else
                {
                    products.DatasetPermit = [dataPermit];
                    productsList.Add(products);
                }
                products = new();
            }
            return productsList;
        }

        /// <summary>
        /// Create ProductKeyServiceRequest from Products
        /// </summary>
        /// <param name="products">Products details.</param>
        /// <returns>ProductKeyServiceRequests</returns>
        private static IEnumerable<ProductKeyServiceRequest> CreateProductKeyServiceRequest(
            IEnumerable<Product> products) =>
            products.Select(p => new ProductKeyServiceRequest
            {
                ProductName = p.ProductName,
                Edition = p.EditionNumber.ToString()
            });

        /// <summary>
        /// Get EncryptedKey from decrypted productkey and HW_ID
        /// </summary>
        /// <param name="decryptedProductKeys">Decrypted keys from product Key with well known hardware id.</param>
        /// <param name="hardwareId">Decrypted HW_ID from Upn.</param>
        /// <param name="cellCode">ProductName</param>
        /// <returns>EncryptedKey</returns>
        private async Task<string> GetEncryptedKeyAsync(IEnumerable<ProductKey> decryptedProductKeys, string hardwareId, string cellCode)
        {
            var decryptedProductKey = decryptedProductKeys.FirstOrDefault(pk => pk.ProductName == cellCode).DecryptedKey;

            return await _s100Crypt.CreateEncryptedKeyAsync(decryptedProductKey, hardwareId);
        }
    }
}