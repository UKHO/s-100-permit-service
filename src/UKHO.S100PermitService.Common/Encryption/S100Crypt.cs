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

        public List<ProductEncKeys> GetEncKeysFromPermitKeys(List<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId)
        {
            _logger.LogInformation(EventIds.GetEncKeysFromPermitKeysStarted.ToEventId(), "Get enc keys from permit keys started.");

            if(hardwareId.Length != KeySizeEncoded)
            {
                throw new PermitServiceException(EventIds.PermitHardwareIdLengthError.ToEventId(), "Expected hardware id length {KeySizeEncoded}, but found {HardwareId Length}.", KeySizeEncoded, hardwareId.Length);
            }

            List<ProductEncKeys> productEncKeys = [];
            foreach(var productKeyServiceResponse in productKeyServiceResponses)
            {
                if(productKeyServiceResponse.Key.Length != KeySizeEncoded)
                {
                    throw new PermitServiceException(EventIds.PermitKeyLengthError.ToEventId(), "Expected permit key length {KeySizeEncoded}, but found {ProductKeyServiceResponse Key Length}.", KeySizeEncoded, productKeyServiceResponse.Key.Length);
                }

                productEncKeys.Add(new ProductEncKeys()
                {
                    ProductName = productKeyServiceResponse.ProductName,
                    Edition = productKeyServiceResponse.Edition,
                    Key = productKeyServiceResponse.Key,
                    EncKey = _aesEncryption.Decrypt(productKeyServiceResponse.Key, hardwareId)
                });
            }

            _logger.LogInformation(EventIds.GetEncKeysFromPermitKeysCompleted.ToEventId(), "Get enc keys from permit keys completed.");

            return productEncKeys;
        }
    }
}