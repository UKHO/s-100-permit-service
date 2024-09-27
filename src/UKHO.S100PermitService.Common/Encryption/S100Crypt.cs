using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private const int KeySizeEncoded = 32;
        private readonly IAesEncryption _aesEncryption;
        private readonly ILogger<S100Crypt> _logger;

        private const int MIdLength = 6;
        private const int EncryptedHardwareIdLength = 32;

        public S100Crypt(IAesEncryption aesEncryption, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GetEncKeysFromPermitKeys(string permitKeys, string hardwareId)
        {
            _logger.LogInformation(EventIds.GetEncKeysFromPermitKeysStarted.ToEventId(), "Get encKeys from permit keys started");

            var encKeys = _aesEncryption.Decrypt(permitKeys, hardwareId);

            _logger.LogInformation(EventIds.GetEncKeysFromPermitKeysCompleted.ToEventId(), "Get encKeys from permit keys completed");

            return encKeys;
        }

        public string GetHwIdFromUserPermit(string upn)
        {
            _logger.LogInformation(EventIds.GetHwIdFromUserPermitStarted.ToEventId(), "Get hardware id from user permit started");

            var encryptedHardwareId = upn[..EncryptedHardwareIdLength];

            // fetch mId from upn
            var mId = upn[^MIdLength..];
            //retrieve mKey from keyvault

            var mKey = "";

            if(mKey?.Length != KeySizeEncoded)
            {
                throw new PermitServiceException(EventIds.InvalidMKey.ToEventId(), "Invalid mKey found");
            }

            var hardwareId = _aesEncryption.Decrypt(encryptedHardwareId, mKey);

            _logger.LogInformation(EventIds.GetHwIdFromUserPermitCompleted.ToEventId(), "Get hardware id from user permit completed");

            return hardwareId;
        }
    }
}