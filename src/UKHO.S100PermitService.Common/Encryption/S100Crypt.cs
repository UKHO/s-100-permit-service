using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.ProductKeyService;
using UKHO.S100PermitService.Common.Models.Request;
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

        /// <summary>
        /// Get decrypted product keys
        /// </summary>
        /// <remarks>
        /// Get Decrypted keys from Product Key Service key and well known hardware id with AES algorithm.
        /// </remarks>
        /// <param name="productKeyServiceResponses">Product Key Service response details.</param>
        /// <param name="hardwareId">Well known hardware id.</param>
        /// <returns>Decrypted product key details.</returns>
        public async Task<IEnumerable<ProductKey>> GetDecryptedKeysFromProductKeysAsync(IEnumerable<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId)
        {
            _logger.LogInformation(EventIds.DecryptProductKeysStarted.ToEventId(), "Decryption of product keys started.");

            var productKeys = new List<ProductKey>();
            foreach(var productKeyServiceResponse in productKeyServiceResponses)
            {
                productKeys.Add(new ProductKey()
                {
                    ProductName = productKeyServiceResponse.ProductName,
                    Edition = productKeyServiceResponse.Edition,
                    Key = productKeyServiceResponse.Key,
                    DecryptedKey = await _aesEncryption.DecryptAsync(productKeyServiceResponse.Key, hardwareId)
                });
            }

            _logger.LogInformation(EventIds.DecryptProductKeysCompleted.ToEventId(), "Decryption of product keys completed.");

            return productKeys;
        }

        /// <summary>
        /// Get decrypted hardware id (HW_ID).
        /// </summary>
        /// <remarks>
        /// Decrypt User Permit(EncryptedHardwareId part of User Permit) and MKey with AES algorithm.
        /// </remarks>
        /// <param name="userPermits">User Permits details.</param>
        /// <returns>Decrypted HardwareIds (HW_ID).</returns>
        public async Task<IEnumerable<UpnInfo>> GetDecryptedHardwareIdFromUserPermitAsync(IEnumerable<UserPermit> userPermits)
        {
            _logger.LogInformation(EventIds.ExtractDecryptedHardwareIdFromUserPermitStarted.ToEventId(), "Extraction of decrypted HW_ID from user permits started.");

            var listOfUpnInfo = new List<UpnInfo>();
            foreach(var userPermit in userPermits)
            {
                var upnInfo = new UpnInfo
                {
                    Upn = userPermit.Upn,
                    Title = userPermit.Title
                };

                var mKey = _manufacturerKeyService.GetManufacturerKeys(userPermit.Upn[^MIdLength..]);
                upnInfo.DecryptedHardwareId = await _aesEncryption.DecryptAsync(userPermit.Upn[..EncryptedHardwareIdLength], mKey);

                listOfUpnInfo.Add(upnInfo);
            }

            _logger.LogInformation(EventIds.ExtractDecryptedHardwareIdFromUserPermitCompleted.ToEventId(), "Extraction of decrypted HW_ID from user permits completed.");

            return listOfUpnInfo;
        }

        /// <summary>
        /// Get EncryptedKey.
        /// </summary>
        /// <remarks>
        /// Encrypt decrypted product key and decrypted HW_ID with AES algorithm.
        /// </remarks>
        /// <param name="productKeyServiceKey">Productkey.</param>
        /// <param name="hardwareId">HardwareId (HW_ID).</param>
        /// <returns>EncryptedKey</returns>
        public async Task<string> CreateEncryptedKeyAsync(string productKeyServiceKey, string hardwareId)
        {
            return await _aesEncryption.EncryptAsync(productKeyServiceKey, hardwareId);
        }
    }
}