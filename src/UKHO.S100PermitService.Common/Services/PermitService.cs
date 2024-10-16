using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
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
        private readonly IOptions<PermitConfiguration> _permitConfiguration;
        private readonly IS100Crypt _s100Crypt;
        private readonly IOptions<ProductKeyServiceApiConfiguration> _productKeyServiceApiConfiguration;

        private readonly string _schemaDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        private readonly string _issueDate = DateTimeOffset.Now.ToString(DateFormat);

        private Dictionary<string, Permit> _permitDictionary = new Dictionary<string, Permit>();

        public PermitService(IPermitReaderWriter permitReaderWriter,
                             ILogger<PermitService> logger,
                             IHoldingsService holdingsService,
                             IUserPermitService userPermitService,
                             IProductKeyService productKeyService,
                             IS100Crypt s100Crypt,
                             IOptions<ProductKeyServiceApiConfiguration> productKeyServiceApiConfiguration,
                             IOptions<PermitConfiguration> permitConfiguration)
        {
            _permitReaderWriter = permitReaderWriter ?? throw new ArgumentNullException(nameof(permitReaderWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _holdingsService = holdingsService ?? throw new ArgumentNullException(nameof(holdingsService));
            _userPermitService = userPermitService ?? throw new ArgumentNullException(nameof(userPermitService));
            _productKeyService = productKeyService ?? throw new ArgumentNullException(nameof(productKeyService));
            _s100Crypt = s100Crypt ?? throw new ArgumentNullException(nameof(s100Crypt));
            _productKeyServiceApiConfiguration = productKeyServiceApiConfiguration ?? throw new ArgumentNullException(nameof(productKeyServiceApiConfiguration));
            _permitConfiguration = permitConfiguration ?? throw new ArgumentNullException(nameof(permitConfiguration));
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

            var listOfUpnInfo = _s100Crypt.GetDecryptedHardwareIdFromUserPermit(userPermitServiceResponse);

            foreach(var upnInfo in listOfUpnInfo)
            {
                var productsList = GetProductsList(holdingsServiceResponse, decryptedProductKeys, upnInfo.DecryptedHardwareId, upnInfo.Title);
                CreatePermitXml(upnInfo.Upn, upnInfo.Title, productsList);
            }

            _logger.LogInformation(EventIds.CreatePermitEnd.ToEventId(), "CreatePermit completed");

            return HttpStatusCode.OK;
        }

        private void CreatePermitXml(string userPermit, string title, List<Products> products)
        {
            var xsdPath = Path.Combine(_schemaDirectory, "XmlSchema", "Permit_Schema.xsd");
            var productsList = new List<Products>();
            productsList.AddRange(products);
            var permit = new Permit
            {
                Header = new Header
                {
                    IssueDate = _issueDate,
                    DataServerIdentifier = _permitConfiguration.Value.DataServerIdentifier,
                    DataServerName = _permitConfiguration.Value.DataServerName,
                    Userpermit = userPermit,
                    Version = ReadXsdVersion()
                },
                Products = [.. productsList]
            };

            _permitDictionary.Add(title, permit);
            _logger.LogInformation(EventIds.XmlSerializationStart.ToEventId(), "Permit Xml serialization started");
            var permitXml = _permitReaderWriter.ReadPermit(permit);
            if(!string.IsNullOrEmpty(permitXml))
            {
                _logger.LogInformation(EventIds.XmlSerializationEnd.ToEventId(), "Permit Xml serialization completed");

                if(ValidateSchema(permitXml, xsdPath))
                {
                    _permitReaderWriter.WritePermit(permitXml);
                    _logger.LogInformation(EventIds.FileCreationEnd.ToEventId(), "Permit Xml file created");
                }
                else
                {
                    _logger.LogError(EventIds.InvalidPermitXmlSchema.ToEventId(), "Invalid xml schema is received");
                }
            }
            else
            {
                _logger.LogError(EventIds.EmptyPermitXml.ToEventId(), "Empty permit xml is received");
            }
        }

        [ExcludeFromCodeCoverage]
        private List<Products> GetProductsList(List<HoldingsServiceResponse> holdings, IEnumerable<ProductKey> productKey, string hardwareId, string UpnTitle)
        {
            var productsList = new List<Products>();
            var products = new Products();

            _logger.LogInformation(EventIds.GetProductListStarted.ToEventId(), "Get Product List details from HoldingServiceResponse and ProductKeyService started for Title: {title}", UpnTitle);

            foreach(var holding in holdings)
            {
                foreach(var cell in holding.Cells.OrderBy(x => x.CellCode))
                {
                    products.Id = string.Format("S-{0}", cell.CellCode.Substring(0, 3));

                    var dataPermit = new ProductsProductDatasetPermit()
                    {
                        EditionNumber = byte.Parse(cell.LatestEditionNumber),
                        EncryptedKey = EncryptKey(productKey, hardwareId, holding),
                        Filename = cell.CellCode,
                        Expiry = holding.ExpiryDate,
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
            _logger.LogInformation(EventIds.GetProductListCompleted.ToEventId(), "Get Product List from HoldingServiceResponse and ProductKeyService completed for title : {title}", UpnTitle);
            return productsList;
        }

        public bool ValidateSchema(string permitXml, string xsdPath)
        {
            var xml = new XmlDocument();
            xml.LoadXml(permitXml);

            var xmlSchemaSet = new XmlSchemaSet();
            xmlSchemaSet.Add(null, xsdPath);

            xml.Schemas = xmlSchemaSet;

            var ValidXml = true;
            try
            {
                xml.Validate((sender, e) =>
                {
                    ValidXml = false;
                });
            }
            catch(XmlSchemaValidationException)
            {
                ValidXml = false;
                return ValidXml;
            }
            return ValidXml;
        }

        private string ReadXsdVersion()
        {
            var xsdPath = Path.Combine(_schemaDirectory, "XmlSchema", "Permit_Schema.xsd");

            XmlSchema? schema;
            using(var reader = XmlReader.Create(xsdPath))
            {
                schema = XmlSchema.Read(reader, null);
            }

            return schema.Version ?? null;
        }

        private List<ProductKeyServiceRequest> ProductKeyServiceRequest(
            IEnumerable<HoldingsServiceResponse> holdingsServiceResponse) =>
            holdingsServiceResponse.SelectMany(x => x.Cells.Select(y => new ProductKeyServiceRequest
            {
                ProductName = y.CellCode,
                Edition = y.LatestEditionNumber
            })).ToList();

        private string EncryptKey(IEnumerable<ProductKey> productKey, string hardwareId, HoldingsServiceResponse holding) =>
            _s100Crypt.CreateEncryptedKey((from str1 in holding.Cells
                                         join str2 in productKey on str1.CellCode.ToString() equals str2.ProductName
                                         select str2.DecryptedKey).FirstOrDefault(), hardwareId);
    }
}