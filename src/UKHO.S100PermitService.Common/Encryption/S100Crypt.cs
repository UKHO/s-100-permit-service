using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private readonly int _keySizeEncoded = 32;
        private readonly IAesEncryption _aesEncryption;
        private readonly ILogger<S100Crypt> _logger;

        public S100Crypt(IAesEncryption aesEncryption, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GetEncKeysFromPermitKeys(string permitKeys, string hardwareId)
        {
            _logger.LogInformation(EventIds.GetEncKeysFromPermitKeysStarted.ToEventId(), "Get encKeys from permit keys started");

            ValidateData(permitKeys, hardwareId);

            var encKeys = _aesEncryption.Decrypt(permitKeys, hardwareId);

            _logger.LogInformation(EventIds.GetEncKeysFromPermitKeysCompleted.ToEventId(), "Get encKeys from permit keys completed");

            return encKeys;
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
            if(upn.Length != _keySizeEncoded)
            {
                throw new PermitServiceException(EventIds.HexLengthError.ToEventId(), "Expected upn data length {0}, but found {1}.",
                                                                                                                _keySizeEncoded, upn.Length);
            }
            if(key.Length != _keySizeEncoded)
            {
                throw new PermitServiceException(EventIds.HexLengthError.ToEventId(), "Expected encoded key length {0}, but found {1}.",
                                                                                                                _keySizeEncoded, key.Length);
            }
            return true;
        }
    }
}