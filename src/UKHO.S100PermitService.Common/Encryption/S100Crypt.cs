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

        public List<ProductKeyServiceResponse> GetEncKeysFromPermitKeys(List<ProductKeyServiceResponse> productKeyServiceResponses, string hardwareId)
        {
            _logger.LogInformation(EventIds.GetEncKeysFromPermitKeysStarted.ToEventId(), "Get enc keys from permit keys started");

            if(hardwareId.Length != KeySizeEncoded)
            {
                throw new PermitServiceException(EventIds.HexLengthError.ToEventId(), "Expected hardware id length {0}, but found {1}.",
                                                                                                                KeySizeEncoded, hardwareId.Length);
            }

            List<ProductKeyServiceResponse> productKeys = [];
            foreach(var productKeyServiceResponse in productKeyServiceResponses)
            {
                if(productKeyServiceResponse.Key.Length != KeySizeEncoded)
                {
                    throw new PermitServiceException(EventIds.HexLengthError.ToEventId(), "Expected permit key length {0}, but found {1}.",
                                                                                                                    KeySizeEncoded, productKeyServiceResponse.Key.Length);
                }

                productKeys.Add(new ProductKeyServiceResponse()
                {
                    ProductName = productKeyServiceResponse.ProductName,
                    Key = productKeyServiceResponse.Key,
                    EncKey = _aesEncryption.Decrypt(productKeyServiceResponse.Key, hardwareId)
                });
            }

            _logger.LogInformation(EventIds.GetEncKeysFromPermitKeysCompleted.ToEventId(), "Get enc keys from permit keys completed");

            return productKeys;
        }

        public string GetHwIdFromUserPermit(string upn, string mKey)
        {
            _logger.LogInformation(EventIds.GetHwIdFromUserPermitStarted.ToEventId(), "Get hardware id from user permit started");

            ValidateData(upn, mKey);

            var hardwareId = _aesEncryption.Decrypt(upn, mKey);

            _logger.LogInformation(EventIds.GetHwIdFromUserPermitCompleted.ToEventId(), "Get hardware id from user permit completed");

            return hardwareId;
        }

        private bool ValidateData(string upn, string key)
        {
            if(upn.Length != KeySizeEncoded)
            {
                throw new PermitServiceException(EventIds.HexLengthError.ToEventId(), "Expected upn data length {0}, but found {1}.",
                                                                                                                KeySizeEncoded, upn.Length);
            }
            if(key.Length != KeySizeEncoded)
            {
                throw new PermitServiceException(EventIds.HexLengthError.ToEventId(), "Expected encoded key length {0}, but found {1}.",
                                                                                                                KeySizeEncoded, key.Length);
            }
            return true;
        }
    }
}