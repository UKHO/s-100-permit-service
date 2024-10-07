using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Events;
using UKHO.S100PermitService.Common.Exceptions;
using UKHO.S100PermitService.Common.Models.UserPermitService;
using UKHO.S100PermitService.Common.Services;

namespace UKHO.S100PermitService.Common.Encryption
{
    public class S100Crypt : IS100Crypt
    {
        private const int KeySizeEncoded = 32;

        private readonly IAesEncryption _aesEncryption;
        private readonly IManufacturerKeyService _manufacturerKeyService;
        private readonly ILogger<S100Crypt> _logger;

        public S100Crypt(IAesEncryption aesEncryption, IManufacturerKeyService manufacturerKeyService, ILogger<S100Crypt> logger)
        {
            _aesEncryption = aesEncryption ?? throw new ArgumentNullException(nameof(aesEncryption));
            _manufacturerKeyService =
                manufacturerKeyService ?? throw new ArgumentNullException(nameof(manufacturerKeyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<UpnInfo> GetDecryptedHardwareIdFromUserPermit(List<UpnInfo> listOfUpnInfo)
        {
            _logger.LogInformation(EventIds.GetHwIdFromUserPermitStarted.ToEventId(), "Get decrypted hardware id from user permits started");

            foreach(var upnInfo in listOfUpnInfo)
            {
                var mKey = _manufacturerKeyService.GetManufacturerKeys(upnInfo.MId);

                if(mKey.Length != KeySizeEncoded)
                {
                    throw new PermitServiceException(EventIds.InvalidMKey.ToEventId(),
                        "Invalid mKey found from Cache/KeyVault, Expected length is {KeySizeEncoded}, but mKey length is {mKeyLength}",
                        KeySizeEncoded, mKey.Length);
                }

                upnInfo.HardwareId = _aesEncryption.Decrypt(upnInfo.EncryptedHardwareId, mKey);
            }

            _logger.LogInformation(EventIds.GetHwIdFromUserPermitCompleted.ToEventId(), "Get decrypted hardware id from user permits completed");

            return listOfUpnInfo;
        }
    }
}