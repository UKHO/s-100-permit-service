using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Xml.Schema;
using System.Xml;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Extensions;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.Holdings;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Validations;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

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

        public PermitService(IPermitReaderWriter permitReaderWriter,
                                ILogger<PermitService> logger,
                                IHoldingsService holdingsService,
                                IUserPermitService userPermitService,
                                IProductKeyService productKeyService)
        {
            _permitReaderWriter = permitReaderWriter ?? throw new ArgumentNullException(nameof(permitReaderWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsService = holdingsService ?? throw new ArgumentNullException(nameof(holdingsService));
            _userPermitService = userPermitService ?? throw new ArgumentNullException(nameof(userPermitService));
            _productKeyService = productKeyService ?? throw new ArgumentNullException(nameof(productKeyService));
        }

        public async Task<HttpStatusCode> CreatePermitAsync(int licenceId, CancellationToken cancellationToken,
            string correlationId)
        {
            _logger.LogInformation(EventIds.CreatePermitStart.ToEventId(), "CreatePermit started");

            var userPermitServiceResponse = await _userPermitService.GetUserPermitAsync(licenceId, cancellationToken, correlationId);

            if(UserPermitServiceResponseValidator.IsResponseNull(userPermitServiceResponse))
            {
                _logger.LogWarning(EventIds.UserPermitServiceGetUserPermitsRequestCompletedWithNoContent.ToEventId(), "Request to UserPermitService responded with empty response");

                return HttpStatusCode.NoContent;
            }

            var holdingsServiceResponse = await _holdingsService.GetHoldingsAsync(licenceId, cancellationToken, correlationId);

            if(ListExtensions.IsNullOrEmpty(holdingsServiceResponse))
            {
                _logger.LogWarning(EventIds.HoldingsServiceGetHoldingsRequestCompletedWithNoContent.ToEventId(), "Request to HoldingsService responded with empty response");

                return HttpStatusCode.NoContent;
            }

            var productsList = GetProductsList(holdingsServiceResponse);

            var productKeyServiceRequest = ProductKeyServiceRequest(holdingsServiceResponse);

            var pksResponseData = await _productKeyService.GetPermitKeysAsync(productKeyServiceRequest, cancellationToken, correlationId);

            foreach(var userPermit in userPermitServiceResponse.UserPermits)
            {
                CreatePermitXml(DateTimeOffset.Now, "AB", "ABC", userPermit.Upn, "1.0", productsList); 
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
            //ValidateSchema(permitXml, (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\\XmlSchema\\Permit.xsd"));
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
        private static List<Products> GetProductsList(List<HoldingsServiceResponse> holdings)
        {
            var issueDate = DateTimeOffset.Now.ToString("yyyy-MM-ddzzz");

            var productsList = new List<Products>();
            var products = new Products();
            foreach(var holding in holdings)
            {
               foreach(var cell in holding.Cells.OrderBy(x=>x.CellCode))
                {
                    products.Id =string.Format("S-{0}",cell.CellCode.Substring(0, 3));

                    var dataPermit = new ProductsProductDatasetPermit()
                    {
                        EditionNumber = byte.Parse(cell.LatestEditionNumber),
                        EncryptedKey = "encryptedkey",
                        Filename = cell.CellCode,
                        Expiry = holding.ExpiryDate,
                        IssueDate = issueDate,
                    };
                    if(productsList.Where(x => x.Id == products.Id).Any())
                    {
                        productsList.FirstOrDefault(x => x.Id == products.Id).DatasetPermit.Add(dataPermit);
                    }
                    else
                    {
                        products.DatasetPermit = new List<ProductsProductDatasetPermit> { dataPermit };
                        productsList.Add(products);
                    }
                    products = new Products();
                }
            }
            return productsList;
        }

        private static List<ProductKeyServiceRequest> ProductKeyServiceRequest(
            IEnumerable<HoldingsServiceResponse> holdingsServiceResponse) =>
            holdingsServiceResponse.SelectMany(x => x.Cells.Select(y => new ProductKeyServiceRequest
            {
                ProductName = y.CellCode,
                Edition = y.LatestEditionNumber
            })).ToList();

        private bool ValidateSchema(string permitXml, string xsdPath)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(permitXml);

            xml.Schemas.Add(null, xsdPath);

            try
            {
                xml.Validate(null);
            }
            catch(XmlSchemaValidationException)
            {
                return false;
            }
            return true;
        }
    }
}