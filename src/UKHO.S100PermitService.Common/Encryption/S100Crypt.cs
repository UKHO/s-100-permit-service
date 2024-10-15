using Microsoft.Extensions.Logging;
using System.Text;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.Permits;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private readonly IAesEncryption _aesEncryption;
        private readonly IManufacturerKeyService _manufacturerKeyService;
        private readonly ILogger<S100Crypt> _logger;

        private const int MIdLength = 6;
        private const int EncryptedHardwareIdLength = 32;

        public S100Crypt(IAesEncryption aesEncryption, IManufacturerKeyService manufacturerKeyService, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _manufacturerKeyService =
                manufacturerKeyService ?? throw new ArgumentNullException(nameof(manufacturerKeyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<ProductKey> GetDecryptedKeysFromProductKeys(IEnumerable<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId)
        {
            _logger.LogInformation(EventIds.GetDecryptedKeysFromProductKeysStarted.ToEventId(), "Get decrypted keys from product keys started.");

            var productKeys = new List<ProductKey>();
            foreach(var productKeyServiceResponse in productKeyServiceResponses)
            {
                productKeys.Add(new ProductKey()
                {
                    ProductName = productKeyServiceResponse.ProductName,
                    Edition = productKeyServiceResponse.Edition,
                    Key = productKeyServiceResponse.Key,
                    DecryptedKey = _aesEncryption.Decrypt(productKeyServiceResponse.Key, hardwareId)
                });
            }

            _logger.LogInformation(EventIds.GetDecryptedKeysFromProductKeysCompleted.ToEventId(), "Get decrypted keys from Product keys completed.");

            return productKeys;
        }

        public IEnumerable<UpnInfo> GetDecryptedHardwareIdFromUserPermit(UserPermitServiceResponse userPermitServiceResponse)
        {
            _logger.LogInformation(EventIds.GetDecryptedHardwareIdFromUserPermitStarted.ToEventId(), "Get decrypted hardware id from user permits started");

            var listOfUpnInfo = new List<UpnInfo>();
            foreach(var userPermit in userPermitServiceResponse.UserPermits)
            {
                var upnInfo = new UpnInfo
                {
                    Upn = userPermit.Upn,
                    Title = userPermit.Title
                };

                var mKey = _manufacturerKeyService.GetManufacturerKeys(userPermit.Upn[^MIdLength..]);
                upnInfo.DecryptedHardwareId = _aesEncryption.Decrypt(userPermit.Upn[..EncryptedHardwareIdLength], mKey);

                listOfUpnInfo.Add(upnInfo);
            }

            _logger.LogInformation(EventIds.GetDecryptedHardwareIdFromUserPermitCompleted.ToEventId(), "Get decrypted hardware id from user permits completed");

            return listOfUpnInfo;
        }       

        public string CreateEncryptedKey(string productKeyServiceKey, string hardwareId)
        {           
           return _aesEncryption.Encrypt(productKeyServiceKey, hardwareId);
        }
    }
 }
