using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.IO;
using UKHO.S100PermitService.Common.Models.PermitService;
using UKHO.S100PermitService.Common.Models.ProductkeyService;

namespace UKHO.S100PermitService.Common.Services
{
    public class PermitService : IPermitService
    {
        private const string DateFormat = "yyyy-MM-ddzzz";

        private readonly ILogger<PermitService> _logger;
        private readonly IPermitReaderWriter _permitReaderWriter;
        private readonly IProductkeyService _productkeyService;

        public PermitService(IPermitReaderWriter permitReaderWriter,
                                ILogger<PermitService> logger,
                                IProductkeyService productkeyService)
        {
            _permitReaderWriter = permitReaderWriter ?? throw new ArgumentNullException(nameof(permitReaderWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _productkeyService = productkeyService ?? throw new ArgumentNullException(nameof(productkeyService));
        }

        public async Task CreatePermit(int licenceId, string correlationId)
        {
            _logger.LogInformation(EventIds.CreatePermitStart.ToEventId(), "CreatePermit started");

            List<ProductKeyServiceRequest> productKeyServiceRequest =
            [
                new ProductKeyServiceRequest()
                {
                    ProductName = "101GB40079ABCDEFG.000",
                    Edition = "1"
                },
                new ProductKeyServiceRequest()
                {
                    ProductName = "102NO32904820801012.h5",
                    Edition = "2"
                },
            ];

            var pksResponseData = await _productkeyService.PostProductKeyServiceRequest(productKeyServiceRequest, correlationId);

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
            var upn = "ABCDEFGHIJKLMNOPQRSTUVYXYZ";

            CreatePermitXml(DateTimeOffset.Now, "AB", "ABC", upn, 1.0m, productsList);

            _logger.LogInformation(EventIds.CreatePermitEnd.ToEventId(), "CreatePermit completed");
        }

        private void CreatePermitXml(DateTimeOffset issueDate, string dataServerIdentifier, string dataServerName, string userPermit, decimal version, List<Products> products)
        {
            var productsList = new List<Products>();
            productsList.AddRange(products);
            var permit = new Permit()
            {
                Header = new Header()
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
    }
}