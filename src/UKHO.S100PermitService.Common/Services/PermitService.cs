using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Encryption;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.UserPermitService;

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

        /// <summary>
        /// Get required data from dependent services and build zip stream containing PERMIT.XML.
        /// </summary>
        /// <remarks>
        /// If dependent services responded with empty response, Then status code 204 NoContent will be returned.
        /// If invalid or non exists licence id requested, Then status code 404 NotFound will be returned.
        /// If duplicate holdings data found, Then remove duplicate dataset and select the dataset with highest expiry date.
        /// If any exception occurred, Then PermitServiceException/AesEncryptionException exception will be thrown.
        /// If any required validation failed, Then PermitServiceException exception will be thrown.
        /// </remarks>
        /// <param name="licenceId">Requested licence id.</param>
        /// <param name="correlationId">Guid based id to track request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <response code="200">Zip stream containing PERMIT.XML.</response>
        /// <response code="204">NoContent - when dependent services responded with empty response.</response>
        /// <response code="404">NotFound - when invalid or non exists licence Id requested.</response>
        /// <response code="500">InternalServerError - exception occurred.</response>
        public async Task<PermitServiceResult> ProcessPermitRequestAsync(int licenceId, string correlationId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(EventIds.ProcessPermitRequestStarted.ToEventId(), "Process permit request started.");

            var userPermitServiceResponseResult = await _userPermitService.GetUserPermitAsync(licenceId, correlationId, cancellationToken);

            var permitServiceResult = HandleServiceResponse(userPermitServiceResponseResult);
            if(permitServiceResult != null)
            {
                return permitServiceResult;
            }

            _userPermitService.ValidateUpnsAndChecksum(userPermitServiceResponseResult.Value);



            var holdingsServiceResponseResult = await _holdingsService.GetHoldingsAsync(licenceId, correlationId, cancellationToken);

            permitServiceResult = HandleServiceResponse(holdingsServiceResponseResult);
            if(permitServiceResult != null)
            {
                return permitServiceResult;
            }

            var holdingsWithLatestExpiry = _holdingsService.FilterHoldingsByLatestExpiry(holdingsServiceResponseResult.Value);

            var productKeyServiceRequest = CreateProductKeyServiceRequest(holdingsWithLatestExpiry);

            var productKeyServiceResponseResult = await _productKeyService.GetProductKeysAsync(productKeyServiceRequest, correlationId, cancellationToken);

            var decryptedProductKeys = await _s100Crypt.GetDecryptedKeysFromProductKeysAsync(productKeyServiceResponseResult.Value, _productKeyServiceApiConfiguration.Value.HardwareId);

            var listOfUpnInfo = await _s100Crypt.GetDecryptedHardwareIdFromUserPermitAsync(userPermitServiceResponseResult.Value);

            var permitDetails = await BuildPermitsAsync(holdingsWithLatestExpiry, decryptedProductKeys, listOfUpnInfo);

            _logger.LogInformation(EventIds.ProcessPermitRequestCompleted.ToEventId(), "Process permit request completed.");

            return PermitServiceResult.Success(permitDetails);
        }

        /// <summary>
        /// Build zip stream containing PERMIT.XML.
        /// </summary>
        /// <remarks>
        /// Generate PERMIT.XML for the respective User Permit Number (UPN) and provides the zip stream containing all the PERMIT.XML.
        /// If any exception occurred, Then PermitServiceException exception will be thrown.
        /// </remarks>
        /// <param name="holdingsServiceResponses">Holding details.</param>
        /// <param name="decryptedProductKeys">Decrypted keys from product Key with well known hardware id.</param>
        /// <param name="upnInfos">User Permit Numbers (UPN) and DecryptedHardwareIds(HW_ID) from EncryptedHardwareIds(Part of UPN) with MKeys.</param>
        /// <returns>Zip stream containing PERMIT.XML.</returns>
        private async Task<Stream> BuildPermitsAsync(IEnumerable<HoldingsServiceResponse> holdingsServiceResponses, IEnumerable<ProductKey> decryptedProductKeys, IEnumerable<UpnInfo> upnInfos)
        {
            var permitDictionary = new Dictionary<string, Permit>();
            var xsdVersion = _permitReaderWriter.ReadXsdVersion();

            foreach(var upnInfo in upnInfos)
            {
                var productsList = await GetProductsListAsync(holdingsServiceResponses, decryptedProductKeys, upnInfo.DecryptedHardwareId);

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
        /// Get product details from HoldingServiceResponse and ProductKeyService
        /// </summary>
        /// <param name="holdingsServiceResponse">Holding details.</param>
        /// <param name="decryptedProductKeys">Decrypted keys from product Key with well known hardware id.</param>
        /// <param name="hardwareId">Decrypted HW_ID from Upn.</param>
        /// <returns>Products</returns>
        [ExcludeFromCodeCoverage]
        private async Task<IEnumerable<Products>> GetProductsListAsync(IEnumerable<HoldingsServiceResponse> holdingsServiceResponse, IEnumerable<ProductKey> decryptedProductKeys, string hardwareId)
        {
            var productsList = new List<Products>();
            var products = new Products();

            foreach(var holding in holdingsServiceResponse)
            {
                foreach(var cell in holding.Datasets.OrderBy(x => x.DatasetName))
                {
                    products.Id = $"S-{cell.DatasetName[..3]}";

                    var dataPermit = new ProductsProductDatasetPermit
                    {
                        EditionNumber = byte.Parse(cell.LatestEditionNumber.ToString()),
                        EncryptedKey = await GetEncryptedKeyAsync(decryptedProductKeys, hardwareId, cell.DatasetName),
                        Filename = cell.DatasetName,
                        Expiry = holding.ExpiryDate
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
            }
            return productsList;
        }

        /// <summary>
        /// Create ProductKeyServiceRequest from HoldingsServiceResponse
        /// </summary>
        /// <param name="holdingsServiceResponse">Holding details.</param>
        /// <returns>ProductKeyServiceRequests</returns>
        private List<ProductKeyServiceRequest> CreateProductKeyServiceRequest(
            IEnumerable<HoldingsServiceResponse> holdingsServiceResponse) =>
            holdingsServiceResponse.SelectMany(x => x.Datasets.Select(y => new ProductKeyServiceRequest
            {
                ProductName = y.DatasetName,
                Edition = y.LatestEditionNumber.ToString()
            })).ToList();

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

        private PermitServiceResult HandleServiceResponse<T>(ServiceResponseResult<T> serviceResponseResult)
        {
            if(!serviceResponseResult.IsSuccess)
            {
                return serviceResponseResult.StatusCode switch
                {
                    HttpStatusCode.BadRequest => PermitServiceResult.BadRequest(serviceResponseResult.ErrorResponse),
                    HttpStatusCode.NotFound => PermitServiceResult.NotFound(serviceResponseResult.ErrorResponse),
                    HttpStatusCode.NoContent => PermitServiceResult.NoContent(),
                    _ => PermitServiceResult.InternalServerError()
                };
            }

            return null;
        }
    }
}