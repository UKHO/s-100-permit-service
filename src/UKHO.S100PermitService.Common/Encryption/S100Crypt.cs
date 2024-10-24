using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private const int MIdLength = 6, EncryptedHardwareIdLength = 32;

        private readonly IAesEncryption _aesEncryption;
        private readonly IManufacturerKeyService _manufacturerKeyService;
        private readonly ILogger<S100Crypt> _logger;

        public S100Crypt(IAesEncryption aesEncryption, IManufacturerKeyService manufacturerKeyService, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _manufacturerKeyService = manufacturerKeyService ?? throw new ArgumentNullException(nameof(manufacturerKeyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<ProductKey> GetDecryptedKeysFromProductKeys(IEnumerable<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId)
        {
            _logger.LogInformation(EventIds.GetDecryptedKeysFromProductKeysStarted.ToEventId(), "Decryption of product keys started.");

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

            _logger.LogInformation(EventIds.GetDecryptedKeysFromProductKeysCompleted.ToEventId(), "Decryption of product keys completed.");

            return productKeys;
        }

        public IEnumerable<UpnInfo> GetDecryptedHardwareIdFromUserPermit(UserPermitServiceResponse userPermitServiceResponse)
        {
            _logger.LogInformation(EventIds.GetDecryptedHardwareIdFromUserPermitStarted.ToEventId(), "Extraction of decrypted HW_ID from user permits started.");

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

            _logger.LogInformation(EventIds.GetDecryptedHardwareIdFromUserPermitCompleted.ToEventId(), "Extraction of decrypted HW_ID from user permits completed.");

            return listOfUpnInfo;
        }

        public string CreateEncryptedKey(string productKeyServiceKey, string hardwareId)
        {
            return _aesEncryption.Encrypt(productKeyServiceKey, hardwareId);
        }
    }
}