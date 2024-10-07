using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.ProductKeyService;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private const int KeySizeEncoded = 32;

        private readonly IAesEncryption _aesEncryption;
        private readonly ILogger<S100Crypt> _logger;

        public S100Crypt(IAesEncryption aesEncryption, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<ProductKey> GetDecryptedKeysFromProductKeys(IEnumerable<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId)
        {
            _logger.LogInformation(EventIds.GetEncKeysFromProductKeysStarted.ToEventId(), "Get enc keys from product keys started.");

            if(hardwareId.Length != KeySizeEncoded)
            {
                throw new PermitServiceException(EventIds.HardwareIdLengthError.ToEventId(), "Expected hardware id length {KeySizeEncoded}, but found {HardwareId Length}.", KeySizeEncoded, hardwareId.Length);
            }

            List<ProductKey> productEncKeys = [];
            foreach(var productKeyServiceResponse in productKeyServiceResponses)
            {
                if(productKeyServiceResponse.Key.Length != KeySizeEncoded)
                {
                    throw new PermitServiceException(EventIds.ProductKeyLengthError.ToEventId(), "Expected product key length {KeySizeEncoded}, but found {ProductKeyServiceResponse Key Length}.", KeySizeEncoded, productKeyServiceResponse.Key.Length);
                }

                productEncKeys.Add(new ProductKey()
                {
                    ProductName = productKeyServiceResponse.ProductName,
                    Edition = productKeyServiceResponse.Edition,
                    Key = productKeyServiceResponse.Key,
                    DecryptedKey = _aesEncryption.Decrypt(productKeyServiceResponse.Key, hardwareId)
                });
            }

            _logger.LogInformation(EventIds.GetEncKeysFromProductKeysCompleted.ToEventId(), "Get enc keys from product keys completed.");

            return productEncKeys;
        }
    }
}