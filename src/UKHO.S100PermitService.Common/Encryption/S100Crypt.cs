using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private readonly IAesEncryption _aesEncryption;
        private readonly ILogger<S100Crypt> _logger;

        public S100Crypt(IAesEncryption aesEncryption, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<ProductKey> GetDecryptedKeysFromProductKeys(IEnumerable<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId)
        {
            _logger.LogInformation(EventIds.GetDecryptedKeysFromProductKeysStarted.ToEventId(), "Get decrypted keys from product keys started.");

            List<ProductKey> productKeys = [];
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

            _logger.LogInformation(EventIds.GetDecryptedKeysFromProductKeysCompleted.ToEventId(), "Get decrypted keys from product keys completed.");

            return productKeys;
        }
    }
}